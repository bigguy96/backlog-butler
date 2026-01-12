var apply = args.Any(a => a.Equals("--apply", StringComparison.OrdinalIgnoreCase));
var dryRun = !apply;

Console.WriteLine("Backlog Butler 🧹");
Console.WriteLine($"Mode: {(dryRun ? "Dry-run" : "Apply")}");
Console.WriteLine();

if (dryRun)
{
    Console.WriteLine("No changes will be made.");
    Console.WriteLine("Use --apply to perform updates.");
}
else
{
    Console.WriteLine("Changes WILL be applied.");
}

return 0;