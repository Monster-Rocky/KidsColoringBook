namespace KidsColoringBook.Models;

public class UserDrawing
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "My Artwork";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public string? OriginalSketchId { get; set; }
    public string? Difficulty { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string ThumbnailPath { get; set; } = string.Empty;
    public bool IsCustomSketch { get; set; }
    public string ChildName { get; set; } = "Rohit";

    public string FormattedDate => ModifiedDate.ToString("dd/MM/yyyy hh:mm tt");
    public string ShortDate => ModifiedDate.ToString("dd/MM/yyyy");
}
