using Microsoft.Data.Sqlite;
#if !ASSIGNMENT_CHECKS
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;
#endif

namespace Assignment12_3_2;

// Question 2: Create, read, update, and delete students with SQLite and .NET MAUI.
public sealed record StudentRecord(long Id, string Name, string Course)
{
    public string DisplayText => $"#{Id}  {Name} | {Course}";
}

public sealed class StudentRepository
{
    private readonly string connectionString;

    public StudentRepository(string databasePath)
    {
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        }.ToString();
    }

    private async Task<SqliteConnection> OpenAsync()
    {
        var connection = new SqliteConnection(connectionString);
        try
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Students (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Course TEXT NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    // READ: return every saved record, ordered by its generated ID.
    public async Task<List<StudentRecord>> GetAllAsync()
    {
        using var connection = await OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Course FROM Students ORDER BY Id;";
        using var reader = await command.ExecuteReaderAsync();
        var students = new List<StudentRecord>();
        while (await reader.ReadAsync())
            students.Add(new StudentRecord(reader.GetInt64(0), reader.GetString(1), reader.GetString(2)));
        return students;
    }

    // CREATE: use parameters so apostrophes and other user input are stored safely.
    public async Task<long> AddAsync(string name, string course)
    {
        Validate(name, course);
        using var connection = await OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Students (Name, Course) VALUES ($name, $course);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$course", course.Trim());
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    // UPDATE: modify only the selected record, identified by its primary key.
    public async Task<bool> UpdateAsync(long id, string name, string course)
    {
        Validate(name, course);
        using var connection = await OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Students SET Name = $name, Course = $course WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name.Trim());
        command.Parameters.AddWithValue("$course", course.Trim());
        return await command.ExecuteNonQueryAsync() == 1;
    }

    // DELETE: remove only the selected record.
    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = await OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Students WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private static void Validate(string name, string course)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(course))
            throw new ArgumentException("Enter both a student name and a course.");
    }
}

#if !ASSIGNMENT_CHECKS
public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder().UseMauiApp<App>().Build();
}

public sealed class App : Application
{
    protected override Window CreateWindow(IActivationState? activationState)
        => new(new NavigationPage(new StudentsPage()))
        {
            Title = "Assignment 12.3.2 - SQLite CRUD",
            Width = 900,
            Height = 700
        };
}

public sealed class StudentsPage : ContentPage
{
    private readonly StudentRepository repository = new(
        Path.Combine(FileSystem.AppDataDirectory, "assignment12_3_students.db3"));
    private readonly Entry nameEntry = new() { Placeholder = "Student name" };
    private readonly Entry courseEntry = new() { Placeholder = "Course" };
    private readonly Label status = new() { Text = "Select a student to edit or delete." };
    private readonly CollectionView students = new() { SelectionMode = SelectionMode.Single };
    private readonly Grid layout;
    private bool busy;

    public StudentsPage()
    {
        Title = "12.3.2 - Student records";
        students.EmptyView = "No students saved. Enter a name and course, then click Add.";
        students.ItemTemplate = new DataTemplate(() =>
        {
            var label = new Label { Padding = new Thickness(12), FontSize = 18 };
            label.SetBinding(Label.TextProperty, nameof(StudentRecord.DisplayText));
            return label;
        });
        students.SelectionChanged += (_, _) =>
        {
            if (SelectedStudent is { } student)
            {
                nameEntry.Text = student.Name;
                courseEntry.Text = student.Course;
                status.Text = $"Editing student #{student.Id}.";
            }
        };

        var buttons = new HorizontalStackLayout { Spacing = 8 };
        AddButton(buttons, "Add", AddAsync);
        AddButton(buttons, "Update", UpdateAsync);
        AddButton(buttons, "Delete", DeleteAsync);
        AddButton(buttons, "Refresh", () => RefreshAsync("Records refreshed."));
        AddButton(buttons, "Clear", () => { ClearEditor(); return Task.CompletedTask; });

        layout = new Grid
        {
            Padding = 24,
            RowSpacing = 12,
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };
        layout.Add(new Label { Text = "SQLite student manager", FontSize = 28 }, 0, 0);
        layout.Add(new Label { Text = "Add a student, select a row to update or delete it. Records remain after you close the app." }, 0, 1);
        layout.Add(nameEntry, 0, 2);
        layout.Add(courseEntry, 0, 3);
        layout.Add(buttons, 0, 4);
        layout.Add(status, 0, 5);
        layout.Add(students, 0, 6);
        Content = layout;
    }

    private StudentRecord? SelectedStudent => students.SelectedItem as StudentRecord;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RunAsync(() => RefreshAsync("Records loaded."));
    }

    private void AddButton(HorizontalStackLayout panel, string text, Func<Task> action)
    {
        var button = new Button { Text = text };
        button.Clicked += async (_, _) => await RunAsync(action);
        panel.Add(button);
    }

    private async Task AddAsync()
    {
        long id = await repository.AddAsync(nameEntry.Text ?? "", courseEntry.Text ?? "");
        ClearEditor();
        await RefreshAsync($"Added student #{id}.");
    }

    private async Task UpdateAsync()
    {
        var student = SelectedStudent ?? throw new ArgumentException("Select a student to update.");
        bool updated = await repository.UpdateAsync(student.Id, nameEntry.Text ?? "", courseEntry.Text ?? "");
        ClearEditor();
        await RefreshAsync(updated ? $"Updated student #{student.Id}." : "Student no longer exists. Records refreshed.");
    }

    private async Task DeleteAsync()
    {
        var student = SelectedStudent ?? throw new ArgumentException("Select a student to delete.");
        if (!await DisplayAlertAsync("Delete student?", $"Delete {student.Name}?", "Delete", "Cancel"))
            return;
        bool deleted = await repository.DeleteAsync(student.Id);
        ClearEditor();
        await RefreshAsync(deleted ? $"Deleted student #{student.Id}." : "Student no longer exists. Records refreshed.");
    }

    private async Task RefreshAsync(string message)
    {
        var records = await repository.GetAllAsync();
        ClearEditor();
        students.ItemsSource = records;
        status.Text = $"{message} {records.Count} student(s) saved.";
    }

    private void ClearEditor()
    {
        students.SelectedItem = null;
        nameEntry.Text = "";
        courseEntry.Text = "";
        status.Text = "Enter a new student, or select one to edit or delete.";
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        layout.IsEnabled = false;
        try { await action(); }
        catch (Exception error) when (error is ArgumentException or SqliteException or IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync("Unable to complete action", error.Message, "OK");
        }
        finally
        {
            layout.IsEnabled = true;
            busy = false;
        }
    }
}
#endif
