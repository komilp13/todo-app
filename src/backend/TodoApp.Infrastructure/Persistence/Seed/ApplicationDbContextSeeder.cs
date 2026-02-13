using TodoApp.Domain.Entities;
using TodoApp.Domain.Enums;
using TaskStatus = TodoApp.Domain.Enums.TaskStatus;

namespace TodoApp.Infrastructure.Persistence.Seed;

/// <summary>
/// Populates the database with seed data for development.
/// </summary>
public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Only seed if database is empty
        if (context.Users.Any())
        {
            return;
        }

        // Create test users
        var user1 = User.Create(
            email: "alice@example.com",
            passwordHash: "hashed_password_1",
            passwordSalt: "salt_1",
            displayName: "Alice Johnson");

        var user2 = User.Create(
            email: "bob@example.com",
            passwordHash: "hashed_password_2",
            passwordSalt: "salt_2",
            displayName: "Bob Smith");

        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        // Create projects for user1
        var project1 = Project.Create(
            userId: user1.Id,
            name: "Q1 Goals",
            description: "Quarterly objectives",
            dueDate: DateTime.UtcNow.AddMonths(3));

        var project2 = Project.Create(
            userId: user1.Id,
            name: "Website Redesign",
            description: "Modernize company website",
            dueDate: DateTime.UtcNow.AddMonths(2));

        context.Projects.AddRange(project1, project2);
        await context.SaveChangesAsync();

        // Create labels for user1
        var workLabel = Label.Create(user1.Id, "Work", "#ff4040");
        var personalLabel = Label.Create(user1.Id, "Personal", "#4073ff");
        var urgentLabel = Label.Create(user1.Id, "Urgent", "#ff9933");
        var readingLabel = Label.Create(user1.Id, "Reading", "#44bb00");

        context.Labels.AddRange(workLabel, personalLabel, urgentLabel, readingLabel);
        await context.SaveChangesAsync();

        // Create tasks for user1 across all system lists
        var tasks = new List<TodoTask>
        {
            // Inbox tasks
            TodoTask.Create(user1.Id, "Review Q1 budget", "Check quarterly spending", SystemList.Inbox, Priority.P2, project1.Id),
            TodoTask.Create(user1.Id, "Send client proposal", "Email proposal to Acme Corp", SystemList.Inbox, Priority.P1),

            // Next tasks
            TodoTask.Create(user1.Id, "Complete project kickoff", "Schedule team meeting", SystemList.Next, Priority.P1, project2.Id),
            TodoTask.Create(user1.Id, "Write technical specification", null, SystemList.Next, Priority.P2, project2.Id),
            TodoTask.Create(user1.Id, "Design homepage mockup", null, SystemList.Next, Priority.P3, project2.Id),

            // Upcoming tasks (with due dates)
            TodoTask.Create(user1.Id, "Q1 performance reviews", "Schedule 1-on-1s", SystemList.Upcoming, Priority.P2, dueDate: DateTime.UtcNow.AddDays(5)),
            TodoTask.Create(user1.Id, "Team lunch planning", "Decide on restaurant", SystemList.Upcoming, Priority.P4, dueDate: DateTime.UtcNow.AddDays(3)),
            TodoTask.Create(user1.Id, "Submit expense report", null, SystemList.Upcoming, Priority.P3, dueDate: DateTime.UtcNow.AddDays(2)),

            // Someday tasks
            TodoTask.Create(user1.Id, "Learn Rust programming", "Online course", SystemList.Someday, Priority.P4),
            TodoTask.Create(user1.Id, "Plan team offsite", "Annual retreat", SystemList.Someday, Priority.P3),

            // Completed tasks (archived)
            TodoTask.Create(user1.Id, "Deploy v2.1 release", null, SystemList.Inbox, Priority.P1, project1.Id),
            TodoTask.Create(user1.Id, "Document API endpoints", null, SystemList.Next, Priority.P2),

            // More tasks for variety
            TodoTask.Create(user1.Id, "Fix login bug", "Users unable to reset password", SystemList.Next, Priority.P1),
            TodoTask.Create(user1.Id, "Update dependencies", null, SystemList.Inbox, Priority.P3),
            TodoTask.Create(user1.Id, "Write unit tests", null, SystemList.Next, Priority.P2),
        };

        // Archive two of them
        var deployTask = tasks[10];
        deployTask.GetType().GetProperty("Status")!.SetValue(deployTask, TaskStatus.Done);
        deployTask.GetType().GetProperty("IsArchived")!.SetValue(deployTask, true);
        deployTask.GetType().GetProperty("CompletedAt")!.SetValue(deployTask, DateTime.UtcNow.AddDays(-5));

        var docTask = tasks[11];
        docTask.GetType().GetProperty("Status")!.SetValue(docTask, TaskStatus.Done);
        docTask.GetType().GetProperty("IsArchived")!.SetValue(docTask, true);
        docTask.GetType().GetProperty("CompletedAt")!.SetValue(docTask, DateTime.UtcNow.AddDays(-2));

        context.Tasks.AddRange(tasks);
        await context.SaveChangesAsync();

        // Assign labels to tasks
        var taskLabels = new List<TaskLabel>
        {
            TaskLabel.Create(tasks[0].Id, workLabel.Id),
            TaskLabel.Create(tasks[0].Id, urgentLabel.Id),
            TaskLabel.Create(tasks[1].Id, workLabel.Id),
            TaskLabel.Create(tasks[2].Id, workLabel.Id),
            TaskLabel.Create(tasks[3].Id, workLabel.Id),
            TaskLabel.Create(tasks[4].Id, workLabel.Id),
            TaskLabel.Create(tasks[5].Id, workLabel.Id),
            TaskLabel.Create(tasks[5].Id, urgentLabel.Id),
            TaskLabel.Create(tasks[6].Id, personalLabel.Id),
            TaskLabel.Create(tasks[7].Id, workLabel.Id),
            TaskLabel.Create(tasks[8].Id, personalLabel.Id),
            TaskLabel.Create(tasks[8].Id, readingLabel.Id),
            TaskLabel.Create(tasks[9].Id, personalLabel.Id),
            TaskLabel.Create(tasks[12].Id, workLabel.Id),
            TaskLabel.Create(tasks[12].Id, urgentLabel.Id),
            TaskLabel.Create(tasks[13].Id, workLabel.Id),
            TaskLabel.Create(tasks[14].Id, workLabel.Id),
        };

        context.TaskLabels.AddRange(taskLabels);
        await context.SaveChangesAsync();

        // Create minimal tasks for user2
        var user2Task1 = TodoTask.Create(user2.Id, "Buy groceries", "Milk, bread, eggs", SystemList.Inbox, Priority.P3);
        var user2Task2 = TodoTask.Create(user2.Id, "Call dentist", "Schedule checkup", SystemList.Inbox, Priority.P3, dueDate: DateTime.UtcNow.AddDays(7));
        var user2Task3 = TodoTask.Create(user2.Id, "Finish book", "Read Chapter 5", SystemList.Someday, Priority.P4);

        context.Tasks.AddRange(user2Task1, user2Task2, user2Task3);
        await context.SaveChangesAsync();
    }
}
