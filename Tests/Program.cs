using Assignment12_3_1;
using Assignment12_3_2;

int checks = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception($"FAILED: {description}");
    checks++;
}

foreach (var (input, expected) in new[]
{
    ("abbaca", "ca"), ("azxxzy", "ay"), ("", ""), ("a", "a"),
    ("aaaa", ""), ("abba", ""), ("abc", "abc"), ("abbba", "aba")
})
    Check(Solution.RemoveDuplicates(input) == expected, $"RemoveDuplicates({input})");
Check(Solution.RemoveDuplicates(new string('a', 100_000)) == "", "Large duplicate input");

// Use an isolated database inside the ignored build directory.
string path = Path.Combine(AppContext.BaseDirectory, $"checks-{Guid.NewGuid():N}.db3");
try
{
    var database = new StudentRepository(path);
    Check((await database.GetAllAsync()).Count == 0, "New database is empty");
    long first = await database.AddAsync("  O'Connor  ", "C#");
    long second = await database.AddAsync("Alex", "SQL");
    Check(first > 0 && second > first, "Unique generated IDs");
    var records = await database.GetAllAsync();
    Check(records.Count == 2 && records[0].Name == "O'Connor", "Create/read and quoted text");
    Check(await database.UpdateAsync(first, "Taylor", "MAUI"), "Update selected record");
    records = await database.GetAllAsync();
    Check(records[0].Name == "Taylor" && records[0].Course == "MAUI" && records[1].Name == "Alex", "Update preserves other record");
    Check((await new StudentRepository(path).GetAllAsync()).Count == 2, "Records persist after reopening");
    string literalSql = "Robert'); DROP TABLE Students;--";
    long third = await database.AddAsync(literalSql, "SQLite");
    Check((await database.GetAllAsync()).Any(s => s.Id == third && s.Name == literalSql), "SQL-like text is stored literally");
    Check(await database.DeleteAsync(first), "Delete selected record");
    records = await database.GetAllAsync();
    Check(records.Count == 2 && records.All(s => s.Id != first), "Delete preserves other records");
    Check(!await database.UpdateAsync(first, "Missing", "Test"), "Updating missing record returns false");
    Check(!await database.DeleteAsync(first), "Deleting missing record returns false");
    bool rejected = false;
    try { await database.AddAsync(" ", "SQL"); }
    catch (ArgumentException) { rejected = true; }
    Check(rejected && (await database.GetAllAsync()).Count == 2, "Blank input is rejected without inserting");
    rejected = false;
    try { await database.UpdateAsync(second, "Alex", " "); }
    catch (ArgumentException) { rejected = true; }
    Check(rejected && (await database.GetAllAsync()).First(s => s.Id == second).Course == "SQL", "Invalid update preserves saved data");
}
finally
{
    if (File.Exists(path)) File.Delete(path);
}
Console.WriteLine($"All {checks} checks passed.");
