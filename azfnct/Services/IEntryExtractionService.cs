using nampacx.docintel.Models;

namespace nampacx.docintel.Services;

public interface IEntryExtractionService
{
    EntryExtractionResultDto ExtractEntries(string jsonPayload);

    EntryExtractionResultDto ExtractEntriesFromFile(string filePath);
}
