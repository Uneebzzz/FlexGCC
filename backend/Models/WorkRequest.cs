namespace WorkRequestTracker.Api.Models;

public enum Priority { Low, Medium, High }
public enum WorkRequestStatus { New, InProgress, Blocked, Completed }

public class WorkRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string? Description { get; set; }
    public Priority Priority { get; set; }
    public WorkRequestStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<WorkRequestNote> Notes { get; set; } = new();
}

public class WorkRequestNote
{
    public int Id { get; set; }
    public int WorkRequestId { get; set; }
    public string Text { get; set; } = "";
    public string? Author { get; set; }
    public DateTime CreatedAt { get; set; }
}
