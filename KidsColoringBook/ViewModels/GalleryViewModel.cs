using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using KidsColoringBook.Models;
using KidsColoringBook.Services;
using KidsColoringBook.Storage;

namespace KidsColoringBook.ViewModels;

public class GalleryViewModel : ViewModelBase
{
    private readonly Action<string, object?> _navigate;
    private List<SketchItem> _allSketches = new();

    private string _selectedDifficulty = "Easy";
    public string SelectedDifficulty
    {
        get => _selectedDifficulty;
        set
        {
            if (SetProperty(ref _selectedDifficulty, value))
            {
                UpdateCategories();
                ApplyFilter();
            }
        }
    }

    private string _selectedCategory = "All";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                ApplyFilter();
            }
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public ObservableCollection<string> Categories { get; } = new();
    public ObservableCollection<SketchItem> FilteredSketches { get; } = new();

    public ICommand SelectDifficultyCommand { get; }
    public ICommand SelectCategoryCommand { get; }
    public ICommand SelectSketchCommand { get; }
    public ICommand BackCommand { get; }

    public GalleryViewModel(Action<string, object?> navigate)
    {
        _navigate = navigate;

        SelectDifficultyCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p is string diff) SelectedDifficulty = diff;
        });

        SelectCategoryCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p is string cat) SelectedCategory = cat;
        });

        SelectSketchCommand = new RelayCommand(p =>
        {
            SoundService.Instance.PlayClick();
            if (p is SketchItem sketch)
            {
                _navigate("Coloring", sketch);
            }
        });

        BackCommand = new RelayCommand(() =>
        {
            SoundService.Instance.PlayClick();
            _navigate("Dashboard", null);
        });

        LoadData();
    }

    public void LoadData()
    {
        _allSketches = StorageService.Instance.LoadAllSketches();
        UpdateCategories();
        ApplyFilter();
    }

    private void UpdateCategories()
    {
        Categories.Clear();
        Categories.Add("All");

        var cats = _allSketches
            .Where(s => string.Equals(s.Difficulty, SelectedDifficulty, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.Category)
            .Distinct()
            .OrderBy(c => c);

        foreach (var c in cats)
        {
            Categories.Add(c);
        }

        if (!Categories.Contains(SelectedCategory))
        {
            _selectedCategory = "All";
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    private void ApplyFilter()
    {
        FilteredSketches.Clear();

        var query = _allSketches.Where(s => string.Equals(s.Difficulty, SelectedDifficulty, StringComparison.OrdinalIgnoreCase));

        if (!string.Equals(SelectedCategory, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => string.Equals(s.Category, SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string term = SearchText.Trim().ToLowerInvariant();
            query = query.Where(s => s.Title.ToLowerInvariant().Contains(term) ||
                                     s.Category.ToLowerInvariant().Contains(term) ||
                                     s.Tags.Any(t => t.ToLowerInvariant().Contains(term)));
        }

        foreach (var item in query)
        {
            FilteredSketches.Add(item);
        }
    }
}
