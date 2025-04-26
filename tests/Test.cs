namespace EasyQTests
{
    // Interface for all test runners
    public interface ITestRunner
    {
        string Name { get; }
        string Description { get; }
        Task RunAsync();
    }

    public class Program
    {
        private static Dictionary<string, ITestRunner> _testRunners = new Dictionary<string, ITestRunner>();
        
        public static async Task Main(string[] args)
        {
            DisplayHeader();
            
            // Register all test runners
            RegisterTestRunners();
            
            // If specific test names were provided, run only those
            if (args.Length > 0)
            {
                await RunSpecificTests(args);
            }
            else
            {
                await RunInteractiveMenu();
            }
        }

        private static void RegisterTestRunners()
        {
            // Register all test runners here
            RegisterTestRunner(new Search.SearchTests());
            RegisterTestRunner(new Cryptography.KeyDistributionTests());
            RegisterTestRunner(new Cryptography.RngTests());
        }
        
        private static void RegisterTestRunner(ITestRunner testRunner)
        {
            _testRunners[testRunner.Name] = testRunner;
        }

        private static async Task RunSpecificTests(string[] testNames)
        {
            Console.WriteLine("Running specified tests...\n");
            
            foreach (var testName in testNames)
            {
                if (_testRunners.TryGetValue(testName, out var runner))
                {
                    await RunTest(runner);
                }
                else
                {
                    Console.WriteLine($"Test not found: {testName}");
                }
            }
        }

        private static async Task RunInteractiveMenu()
        {
            while (true)
            {
                DisplayTestMenu();
                
                Console.Write("\nEnter a test number to run (or 'q' to quit): ");
                var input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "q")
                {
                    break;
                }
                
                if (int.TryParse(input, out int testIndex) && testIndex > 0 && testIndex <= _testRunners.Count)
                {
                    var runner = _testRunners.Values.ElementAt(testIndex - 1);
                    await RunTest(runner);
                    
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
                else if (input.ToLower() == "all")
                {
                    await RunAllTests();
                    
                    Console.WriteLine("\nPress any key to continue...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                }
            }
        }

        private static void DisplayHeader()
        {
            Console.WriteLine("=============================================");
            Console.WriteLine("               EasyQ Test Suite              ");
            Console.WriteLine("=============================================");
            Console.WriteLine("Testing quantum operations with a developer-friendly API\n");
        }

        private static void DisplayTestMenu()
        {
            Console.Clear();
            DisplayHeader();
            
            Console.WriteLine("Available Tests:");
            Console.WriteLine("---------------");
            
            int index = 1;
            foreach (var runner in _testRunners.Values)
            {
                Console.WriteLine($"{index}. {runner.Name}");
                Console.WriteLine($"   {runner.Description}");
                Console.WriteLine();
                index++;
            }
            
            Console.WriteLine("Type 'all' to run all tests");
            Console.WriteLine("Type 'q' to quit");
        }

        private static async Task RunTest(ITestRunner runner)
        {
            Console.WriteLine($"\nRunning: {runner.Name}");
            Console.WriteLine(new string('-', runner.Name.Length + 9));
            
            var startTime = DateTime.Now;
            
            try
            {
                await runner.RunAsync();
                
                var duration = DateTime.Now - startTime;
                Console.WriteLine($"\nTest completed in {duration.TotalSeconds:F1} seconds");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nTest failed with error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static async Task RunAllTests()
        {
            Console.WriteLine("\nRunning all tests...\n");
            
            int passed = 0;
            int failed = 0;
            
            foreach (var runner in _testRunners.Values)
            {
                try
                {
                    await RunTest(runner);
                    passed++;
                    Console.WriteLine($"{runner.Name}: Passed");
                }
                catch (Exception)
                {
                    failed++;
                    Console.WriteLine($"{runner.Name}: Failed");
                }
                
                Console.WriteLine(new string('-', 40));
            }
            
            Console.WriteLine($"\nSummary: {passed} passed, {failed} failed, {passed + failed} total");
        }
    }
}