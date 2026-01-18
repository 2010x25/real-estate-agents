using PuppeteerSharp;
using Shared;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        var outputFilePath = @"D:\MyProjects\PropertyAgentsForce\Agent.Console\property_listings.json";

        var options = new LaunchOptions
        {
            Headless = false,
            ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
            Args = ["--start-maximized", "--disable-blink-features=AutomationControlled"]
        };

        var browser = await Puppeteer.LaunchAsync(options);
        var urls = await File.ReadAllLinesAsync("input-urls.txt");
        var listings = new List<PropertyDetail>();
        foreach (var url in urls)
        {
            if(string.IsNullOrEmpty(url)) continue;

            await using var page = await browser.NewPageAsync();
            await page.GoToAsync(url, WaitUntilNavigation.Networkidle2);
            await page.WaitForSelectorAsync("footer");

            var data = await page.EvaluateFunctionAsync<PropertyDetail>(@"
                () => {                
                const title =
                    document.getElementsByClassName('property-info-address')[0]?.innerText ?? null;
                
                const rooms =
                    document
                        .getElementsByClassName('styles__Wrapper-sc-xhfhyt-1 iroawY property-info__primary-features')[0]
                        ?.getAttribute('aria-label') ?? null;
                
                const status =
                    document
                        .getElementsByClassName('property-info__footer-content')[0]
                        ?.getElementsByTagName('p')[0]
                        ?.innerText ?? null;
                
                const description =
                    document.querySelector('[data-testid=PropertyDescription]')?.innerText ?? null;
                
                const nearbySchools =
                    Array.from(
                        document.querySelectorAll('span.nearby-schools__name')
                    ).map(x => x.innerText);
                
                const agentName =
                    document
                        .getElementsByClassName('agent-info__contact-info')[0]
                        ?.getElementsByTagName('a')[0]
                        ?.innerText ?? null;
                
                const address =
                    document
                        .getElementsByClassName('sidebar-traffic-driver contact-agent-panel__traffic-driver')[0]
                        ?.innerText ?? null;

                return {
                    title,
                    rooms,
                    status,
                    description,
                    nearbySchools,
                    agentName,
                    address
                };
            }
        ");
            listings.Add(data);

            Console.WriteLine($"Title: {data.Title}");
            Console.WriteLine($"Rooms: {data.Rooms}");
            Console.WriteLine($"Status: {data.Status}");
            Console.WriteLine($"Description: {data.Description}");
            Console.WriteLine($"Agent: {data.AgentName}");
            Console.WriteLine($"Address: {data.Address}");

            Console.WriteLine("Nearby Schools:");
            data.NearbySchools.ForEach(s => Console.WriteLine($" - {s}"));
            await page.CloseAsync();
            await Task.Delay(5000);
        }

        var listingsData = JsonSerializer.Serialize(listings);
        await File.WriteAllTextAsync(outputFilePath, listingsData);

        await browser.CloseAsync();
    }
}
