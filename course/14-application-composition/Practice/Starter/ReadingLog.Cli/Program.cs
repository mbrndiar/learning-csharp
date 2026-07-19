using LearningCSharp.Course.Unit14.Practice.Application;
using LearningCSharp.Course.Unit14.Practice.Cli;

var command = new SummaryCommand(new SummaryApplication(new JsonReadingLogSource()));
return await command.RunAsync(args, Console.Out, Console.Error);
