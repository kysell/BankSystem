using System.Diagnostics;
using BankSystem.App.Services;
using BankSystem.Data.Storages;
using BankSystem.Domain.Models;

if (args.Contains("--postgres-smoke"))
{
    await PostgresSmoke.RunAsync();
    return;
}

if (args.Contains("--currency-smoke"))
{
    using var http = new HttpClient
    {
        BaseAddress = new Uri("https://api.frankfurter.dev/"),
        Timeout = TimeSpan.FromSeconds(15)
    };
    try
    {
        decimal result = await new CurrencyService(http).ConvertAsync(10, "USD", "EUR");
        Console.WriteLine($"Live CurrencyService: 10 USD = {result} EUR");
    }
    catch (OperationCanceledException)
    {
        Console.Error.WriteLine("Currency API did not respond within the timeout. Try again later.");
        Environment.ExitCode = 1;
    }
    catch (HttpRequestException exception)
    {
        Console.Error.WriteLine($"Currency API request failed: {exception.Message}");
        Environment.ExitCode = 1;
    }
    return;
}

var generator = new TestDataGenerator();
var clients = generator.CreateClients();
var employees = generator.CreateEmployees();
Console.WriteLine($"Bogus sample: {generator.CreateRandomClients(1)[0].FullName}");
var phoneIndex = clients.ToDictionary(x => x.PhoneNumber);
var phone = clients[^1].PhoneNumber;
var measurements = TestDataGenerator.MeasurePhoneSearch(clients, phoneIndex, phone);
Console.WriteLine($"List search: {measurements.List.Ticks} ticks; Dictionary key: {measurements.Dictionary.Ticks} ticks.");
var watch = Stopwatch.StartNew();
var last = phoneIndex.LastOrDefault(p => p.Key == phone).Value;
watch.Stop();
Console.WriteLine($"Dictionary.LastOrDefault: {watch.Elapsed.Ticks} ticks; same client: {last == phoneIndex[phone]}");
watch.Restart();
Client byKey = phoneIndex[phone];
watch.Stop();
Console.WriteLine($"Dictionary key again: {watch.Elapsed.Ticks} ticks; same client: {last == byKey}");
List<Client> youngerClients = clients.Where(client => client.Age < 30).ToList();
Employee lowestPaidEmployee = employees.MinBy(item => item.Salary)!;
Console.WriteLine($"Clients younger than 30: {youngerClients.Count}; minimum salary: {lowestPaidEmployee.Salary}");
var bank = new BankService();
var employee = employees[0];
employee.Position = "Engineer";
bank.UpdateEmployeeContract(employee);
Console.WriteLine(employee.Contract);
var currency = new CurrencyValue("USD", 1);
var copy = currency;
copy.Code = "GBP";
Console.WriteLine($"Struct copy: original {currency.Code}; copy {copy.Code}");
bank.UpdateCurrency(currency, "GBP", .8m);
Console.WriteLine($"After method by value: {currency.Code}");
bank.UpdateCurrency(ref currency, "EUR", .92m);
Console.WriteLine($"Struct by ref: {currency}");
Person person = clients[0];
Console.WriteLine($"is Client: {person is Client}; as Employee is null: {person as Employee is null}");
Console.WriteLine($"Mapped employee: {bank.ConvertClientToEmployee(clients[0]).FullName}");
employee.IsBankOwner = true;
Console.WriteLine($"Owner salary with explicit decimal -> int: {bank.CalculateOwnersSalary(10001, 1000, [employee])}");
// Сохраняем упакованные объекты и читаем значения, чтобы JIT не убрал эти действия.
var boxed = new object[100_000];
watch.Restart();
for (var i = 0; i < boxed.Length; i++)
{
    boxed[i] = i;
}

watch.Stop();
Console.WriteLine($"Boxing: {watch.Elapsed.Ticks} ticks");
long sum = 0;
watch.Restart();
for (var i = 0; i < boxed.Length; i++)
{
    sum += (int)boxed[i];
}

watch.Stop();
Console.WriteLine($"Unboxing: {watch.Elapsed.Ticks} ticks; sum {sum}");
Console.WriteLine("Stopwatch is an educational measurement; timings depend on JIT and machine load.");
IAnimal[] animals = [new Dog("Rex"), new Cat("Mia")];
foreach (var animal in animals)
{
    Console.WriteLine(animal.Speak());
}

var circles = Enumerable.Range(1, 10).Select(i => new Circle(i)).ToArray();
Array.Sort(circles, new CircleAreaDescendingComparer());
Console.WriteLine($"10 circles descending: {string.Join(", ", circles.Select(c => c.Radius))}");
Console.WriteLine($"Sort without CompareTo: {string.Join(", ", clients.OrderBy(c => c.FirstName).Take(3).Select(c => c.FirstName))}");
Console.WriteLine($"Func: {DelegateExamples.Apply(x => x * 2, 3)}; Predicate: {string.Join(",", DelegateExamples.Filter([1, 2, 3], x => x > 1))}");
DelegateExamples.InvokeThenRemove(() => Console.WriteLine("Action retained"), () => Console.WriteLine("removed"));
var queue = new ThresholdQueue<int>(1);
EventHandler threshold = (_, _) => Console.WriteLine("Queue threshold exceeded");
EventHandler empty = (_, _) => Console.WriteLine("Queue became empty");
queue.ThresholdReached += threshold;
queue.BecameEmpty += empty;
queue.Enqueue(1);
queue.Enqueue(2);
queue.Dequeue();
queue.Dequeue();
queue.ThresholdReached -= threshold;
queue.BecameEmpty -= empty;
var observable = new ObservableBalance();
System.ComponentModel.PropertyChangedEventHandler changed = (_, e) => Console.WriteLine($"Property changed: {e.PropertyName}");
observable.PropertyChanged += changed;
observable.Amount = 10;
observable.PropertyChanged -= changed;
var analyzer = new NumberStreamAnalyzer(10);
EventHandler<NumberChangedEventArgs> exceeded = (_, e) => Console.WriteLine($"Number difference exceeded: {e.Value}");
analyzer.DifferenceExceeded += exceeded;
analyzer.Add(100);
analyzer.Add(120);
analyzer.DifferenceExceeded -= exceeded;
var storage = new ClientStorage();
var service = new ClientService(storage);
await service.AddClientAsync(clients[0]);
Console.WriteLine($"Added {clients[0].FullName} with default {clients[0].Accounts[0].Currency.Code} account.");
