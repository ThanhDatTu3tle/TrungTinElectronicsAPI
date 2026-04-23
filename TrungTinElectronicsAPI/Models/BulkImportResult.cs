namespace TrungTinElectronicsAPI.Models;

public class BulkImportResult
{
    public int SuccessCount { get; set; }
    public List<BulkImportError> Errors { get; set; } = new();
}

public class BulkImportError
{
    public int Row { get; set; }
    public string Code { get; set; } = "";
    public string Reason { get; set; } = "";
}
