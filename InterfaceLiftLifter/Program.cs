using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;

//TODO:
// Convert streamwriter to IDisposable?
// Convert output to JSON - text file has IDs and URL with Fowarded URL in it in a JSON format
// Instructions on how to find first URL
// Instructions on useragent

namespace InterfaceLiftLifter
{
    class Program
    {
        static void Main(string[] args)
        {
            string firstURL = "https://interfacelift.com/wallpaper/downloads/downloads/wide_16:9/2560x1440/index1.html";
            string userAgent = "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 74.0.3729.169 Safari / 537.36";
            int startingPageNumber = 1;

            IWebDriver driver;
            WebDriverWait wait;
            Random waitTime = new Random();
            StreamWriter outfile = new StreamWriter("InterfaceliftLifter_" + DateTime.Now.ToFileTime() + ".txt"); //
            int failCount = 0;
            int failThreshold = 5;
            int totalFound = 0;
            string numberOfPagesXPath = "//*[@id='page']/div[4]/div[1]/p[2]/b[2]";
            string currentPageXPath = "//*[@id='page']/div[4]/div[1]/p[2]/b[1]";
            string linksFromPageXPath = "//*[contains(@id,'download_')]/a";

            string folderName = Regex.Match(firstURL, @"(\d+)x(\d+)(?=\/)").Success ? Regex.Match(firstURL, @"(\d+)x(\d+)(?=\/)").Value : "2560x1440" ;
            string pageTitle = "InterfaceLIFT: " + folderName + " Wallpaper sorted by Downloads";

            //Make the folder to hold our output
            System.IO.Directory.CreateDirectory(folderName);

            using (driver = new ChromeDriver())
            {
                //Wait for driver to start up
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                //Go to first page and wait for it to load
                driver.Navigate().GoToUrl(firstURL);
                wait.Until<bool>((d) => { return d.Title.Contains(pageTitle); });

                //Get number of pages by grabbing "You are on Page 1 of 229"
                int numofpages = Convert.ToInt32(driver.FindElement(By.XPath(numberOfPagesXPath)).Text);

                for (int count = startingPageNumber; count <= numofpages; count++)
                {
                    //If we're not on the first run, 
                    if(count > 1)
                    {
                        //load the next page
                        driver.Navigate().GoToUrl(@"https://interfacelift.com/wallpaper/downloads/downloads/wide_16:9/2560x1440/index" + count + ".html");

                        //Wait until its loaded
                        wait.Until<bool>((d) => { return d.Title.Contains(pageTitle); });

                        //Grab the current page number and write it
                        var currentpage = driver.FindElement(By.XPath(currentPageXPath)).Text;
                        Console.WriteLine("Loaded " + currentpage);
                    }

                    //Get all links on the page
                    var linksonpage = driver.FindElements(By.XPath(linksFromPageXPath));
                    var linkscount = linksonpage.Count();

                    //if we didnt find anything go to the next page
                    if (linkscount == 0)
                    {
                        failCount++;
                        continue;
                    }
                    else
                    {
                        totalFound += linkscount;
                    }

                    //For each link we found
                    foreach (var link in linksonpage)
                    {
                        //grab the url and write it
                        string downloadurl = link.GetAttribute("href");
                        outfile.WriteLine(downloadurl);

                        //Get the image file name(We'll need this for downloading)
                        Uri uri = new Uri(downloadurl);
                        string filename = System.IO.Path.GetFileName(uri.LocalPath);

                        //Check to see if the image file already exists
                        if(File.Exists(folderName + "/" + filename))
                        {
                            Console.WriteLine("Image file already Exists. Skipping.");
                            continue;
                        }

                        //Wait between 1 and 10 seconds before downloading
                        var waittime = (waitTime.Next(1,11)) * 1000;
                        Thread.Sleep(waittime);
                        Debug.WriteLine("Pausing for " + waittime);
                        
                        try
                        {
                            //Request the URL with a useragent set to get the actual location of the image
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadurl);
                            request.UserAgent = userAgent;
                            request.AllowAutoRedirect = false;
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                            string imgLocation = response.Headers["Location"];
                            response.Close();

                            //Download the image
                            using (WebClient webClient = new WebClient())
                            {
                                webClient.DownloadFile(imgLocation, folderName+ "/" + filename);
                                Console.WriteLine("Downloaded Image!");
                            }
                        }
                        catch(Exception ex)
                        {
                            outfile.WriteLine("Failed to download - " + downloadurl);
                            outfile.WriteLine(ex.InnerException);
                            Console.WriteLine("Failed to Downloaded Image!");
                        }

                    }

                    //Make sure we're all written
                    outfile.Flush();
                                       
                    //Wait between 1 and 10 amount of time before going to the next page
                    Thread.Sleep((waitTime.Next(1, 11)) * 1000);

                    Console.WriteLine("Finished Page " + count + " of " + numofpages + ", total links found: " + totalFound);

                    //If fail count gets over threshold we should stop
                    if(failCount >= failThreshold)
                    {
                        Console.WriteLine("Threshold reached, failing!");
                        break;
                    }
                }

                Console.WriteLine("Done!");

                //Close web driver using Quit
                driver.Quit();

                //flush and close our streamwriter
                outfile.Flush();
                outfile.Close();
            }

        }
    }
}