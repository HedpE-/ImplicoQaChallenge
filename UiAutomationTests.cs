using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ImplicoQaChallenge
{
    public class UiAutomationTests
    {
        #region Private members

        private string DriverFilePath;
        private IWebDriver Driver;
        private WebDriverWait Wait;

        private readonly string DriverBinaryFileName = "chromedriver.exe";
        private readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

        private readonly string StartUrl = "https://www.saucedemo.com/";

        #endregion Private members

        [SetUp]
        public void Setup()
        {
            InitializeSeleniumDriver(true, false);
        }

        [TearDown]
        public void TearDown()
        {
            StopSeleniumDriver();
        }

        [Test]
        public void TC01_Login_Logout()
        {
            Driver.Url = StartUrl;
            DoLogin();
            DoLogout();
        }

        [Test]
        public void TC02_AddItemToCart()
        {
            Driver.Url = StartUrl;
            DoLogin();

            AddRandomItemToCart();

            DoLogout();
        }

        [Test]
        public void TC03_RemoveItemFromCart()
        {
            Driver.Url = StartUrl;
            DoLogin();

            AddRandomItemToCart();
            AddRandomItemToCart();

            IWebElement element = GetElement(By.ClassName("shopping_cart_link"));
            element.Click();

            IEnumerable<IWebElement> elements = Driver.FindElements(By.ClassName("cart_item"));
            Random rnd = new Random();
            int itemIndex = rnd.Next(elements.Count());
            element = GetElement(elements.ElementAt(itemIndex).FindElement(By.TagName("button")));
            element.Click();

            element = GetElement(By.Id("continue-shopping"));
            element.Click();

            DoLogout();
        }

        [Test]
        public void TC04_ViewItemDetails()
        {
            Driver.Url = StartUrl;
            DoLogin();

            IEnumerable<IWebElement> elements = Driver.FindElements(By.ClassName("inventory_item"));
            Random rnd = new Random();
            int itemIndex = rnd.Next(elements.Count());
            IWebElement element = GetElement(elements.ElementAt(itemIndex).FindElement(By.ClassName("inventory_item_label")).FindElement(By.TagName("a")));
            element.Click();

            element = GetElement(By.Id("back-to-products"));
            element.Click();

            DoLogout();
        }

        [Test]
        public void TC05_StartCheckout()
        {
            Driver.Url = StartUrl;
            DoLogin();

            AddRandomItemToCart();
            AddRandomItemToCart();

            IWebElement element = GetElement(By.ClassName("shopping_cart_link"));
            element.Click();

            element = GetElement(By.Id("checkout"));
            element.Click();

            element = GetElement(By.Id("first-name"));
            element.SendKeys("Rui");

            element = GetElement(By.Id("last-name"));
            element.SendKeys("Gonçalves");

            element = GetElement(By.Id("postal-code"));
            element.SendKeys("2460-602");

            element = GetElement(By.Id("continue"));
            element.Click();

            element = GetElement(By.Id("finish"));
            element.Click();

            element = GetElement(By.Id("back-to-products"));
            element.Click();

            DoLogout();
        }

        #region Private methods
        private void DoLogin()
        {
            IWebElement element = GetElement(By.Id("user-name"));
            element.SendKeys("standard_user");
            element = GetElement(By.Id("password"));
            element.SendKeys("secret_sauce");
            element = GetElement(By.Id("login-button"));
            element.Click();
        }

        private void DoLogout()
        {
            IWebElement element = GetElement(By.Id("react-burger-menu-btn"));
            element.Click();
            element = GetElement(By.Id("logout_sidebar_link"));
            element.Click();
        }

        private void AddRandomItemToCart()
        {
            IEnumerable<IWebElement> elements = Driver.FindElements(By.ClassName("inventory_item"));
            Random rnd = new Random();
            int itemIndex = rnd.Next(elements.Count());
            IWebElement element = GetElement(elements.ElementAt(itemIndex).FindElement(By.TagName("button")));
            element.Click();
        }

        private IWebElement GetElement(By locator)
        {
            IWebElement element = Wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(locator));
            return element;
        }

        private IWebElement GetElement(IWebElement element)
        {
            element = Wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementToBeClickable(element));
            return element;
        }

        private void InitializeSeleniumDriver(bool hideCommandPromptWindow, bool headless, string driverFilePath = null)
        {
            if (!string.IsNullOrEmpty(driverFilePath))
            {
                if (!IsValidPath(driverFilePath))
                    throw new ArgumentException($"driverFilePath value '{driverFilePath}' is invalid.");

                if (string.IsNullOrEmpty(Path.GetFileName(driverFilePath)))
                    driverFilePath = Path.Combine(driverFilePath, DriverBinaryFileName);
                else
                {
                    FileInfo file = new FileInfo(driverFilePath);
                    if (file.Extension.ToLower() != ".exe")
                        throw new ArgumentException($"driverFilePath value '{driverFilePath}' is invalid. File extension '{file.Extension}' is not an executable type.");
                }
            }
            else
                driverFilePath = Path.Combine(Path.GetTempPath(), DriverBinaryFileName);

            DriverFilePath = driverFilePath;

            if (File.Exists(driverFilePath))
            {
                try
                {
                    File.Delete(driverFilePath);
                }
                catch (Exception e)
                {
                    if (e is UnauthorizedAccessException)
                    {
                        var allProcesses = Process.GetProcesses().Where(p =>
                        {
                            try { return p.MainModule.FileName == driverFilePath; } catch { return false; }
                        });
                        foreach (Process proc in allProcesses)
                        {
                            try
                            {
                                proc.Kill();
                                Thread.Sleep(2000);
                                File.Delete(driverFilePath);
                            }
                            catch
                            {
                                throw;
                            }
                        }
                    }
                }
            }

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            ExtractResourceToLocalFile(assemblyName + "." + DriverBinaryFileName, assemblyName, driverFilePath);

            ChromeOptions options = new ChromeOptions();

            options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            options.AddArgument("ignore-certificate-errors");
            options.AddArgument("start-maximized");
            if (headless)
                options.AddArgument("--headless");
            if (hideCommandPromptWindow)
            {
                var driverService = ChromeDriverService.CreateDefaultService(Path.GetDirectoryName(driverFilePath), Path.GetFileName(driverFilePath));
                driverService.HideCommandPromptWindow = hideCommandPromptWindow;
                Driver = new ChromeDriver(driverService, options);
            }
            else
                Driver = new ChromeDriver(options);

            Wait = new WebDriverWait(Driver, WaitTimeout);
        }

        private void StopSeleniumDriver()
        {
            Driver?.Quit();
            Driver?.Dispose();
            var processes = Process.GetProcessesByName(DriverBinaryFileName.Replace(".exe", ""));
            if (processes.Any())
            {
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                    catch { }
                }
            }

            if (File.Exists(DriverFilePath))
                File.Delete(DriverFilePath);
        }

        private string ExtractResourceToLocalFile(string resourceName, string assemblyName, string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
                filePath = Path.GetTempFileName();

            var assembly = GetAssemblyByName(assemblyName);

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (MemoryStream reader = new MemoryStream())
            {
                stream.CopyTo(reader);
                File.WriteAllBytes(filePath, reader.ToArray());
            }

            return filePath;
        }

        private Assembly GetAssemblyByName(string name)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SingleOrDefault(assembly => assembly.GetName().Name == name);
        }

        private bool IsValidPath(string pathString)
        {
            if (string.IsNullOrWhiteSpace(pathString) || pathString.Length < 3)
                return false;

            Regex driveCheck = new Regex(@"^[a-zA-Z]:\\$");
            if (!driveCheck.IsMatch(pathString.Substring(0, 3)))
                return false;

            string strTheseAreInvalidFileNameChars = new string(Path.GetInvalidPathChars());
            strTheseAreInvalidFileNameChars += @":/?*" + "\"";
            Regex containsABadCharacter = new Regex("[" + Regex.Escape(strTheseAreInvalidFileNameChars) + "]");
            if (containsABadCharacter.IsMatch(pathString.Substring(3, pathString.Length - 3)))
                return false;

            string tempPath = Path.GetFullPath(pathString);
            if (!string.IsNullOrEmpty(Path.GetFileName(pathString)))
                tempPath = tempPath.Replace(Path.GetFileName(pathString), "");
            DirectoryInfo directoryInfo = new DirectoryInfo(tempPath);
            if (!directoryInfo.Exists)
                return false;

            return true;
        }
        #endregion Private methods
    }
}