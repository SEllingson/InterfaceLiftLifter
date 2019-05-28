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

namespace InterfaceLiftLifter
{
    class Program
    {
        static void Main(string[] args)
        {
            IWebDriver driver;
            WebDriverWait wait;
            Random waitTime = new Random();
            StreamWriter outfile = new StreamWriter("InterfaceliftLifter_" + DateTime.Now.ToFileTime() + ".txt");
            int failcount = 0;
            int failthreshold = 5;
            int totalfound = 0;

            using (driver = new ChromeDriver())
            {
                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

                //Go to first page and wait for it to load
                driver.Navigate().GoToUrl(@"https://interfacelift.com/wallpaper/downloads/downloads/wide_16:9/2560x1440/index225.html");
                wait.Until<bool>((d) => { return d.Title.Contains("InterfaceLIFT: 2560x1440 Wallpaper sorted by Downloads"); });

                //Get number of pages by grabbing "You are on Page 1 of 229"
                int numofpages = Convert.ToInt32(driver.FindElement(By.XPath("//*[@id='page']/div[4]/div[1]/p[2]/b[2]")).Text);

                for (int count = 225; count <= numofpages; count++)
                {
                    //If we're not on the first run, 
                    if(count > 1)
                    {
                        //load the next page
                        driver.Navigate().GoToUrl(@"https://interfacelift.com/wallpaper/downloads/downloads/wide_16:9/2560x1440/index" + count + ".html");

                        //Wait until its loaded
                        wait.Until<bool>((d) => { return d.Title.Contains("InterfaceLIFT: 2560x1440 Wallpaper sorted by Downloads"); });

                        //Grab the current page number and write it
                        var currentpage = driver.FindElement(By.XPath("//*[@id='page']/div[4]/div[1]/p[2]/b[1]")).Text;
                        Console.WriteLine("Loaded " + currentpage);
                    }

                    //Get all links on the page
                    var linksonpage = driver.FindElements(By.XPath("//*[contains(@id,'download_')]/a"));
                    var linkscount = linksonpage.Count();

                    //if we didnt find anything go to the next page
                    if (linkscount == 0)
                    {
                        failcount++;
                        continue;
                    }
                    else
                    {
                        totalfound += linkscount;
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
                        if(File.Exists("2560x1440/" + filename))
                        {
                            Console.WriteLine("Image file already Exists. Skipping.");
                            continue;
                        }

                        var waittime = (waitTime.Next(1,6)) * 1000;

                        //Wait between 1 and 10 seconds before downloading
                        Thread.Sleep(waittime);
                        Debug.WriteLine("Pausing for " + waittime);
                        
                        try
                        {
                            //Request the URL with a useragent set to get the actual location of the image
                            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downloadurl);
                            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/74.0.3729.169 Safari/537.36";
                            request.AllowAutoRedirect = false;
                            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                            string imgLocation = response.Headers["Location"];
                            response.Close();

                            //Download the image
                            using (WebClient webClient = new WebClient())
                            {
                                webClient.DownloadFile(imgLocation, "2560x1440/" + filename);
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

                    Console.WriteLine("Finished Page " + count + " of " + numofpages + ", total links found: " + totalfound);

                    if(failcount >= failthreshold)
                    {
                        Console.WriteLine("Threshold reached, failing!");
                        break;
                    }

                }

                Console.WriteLine("Done!");

                driver.Close();

                outfile.Flush();
                outfile.Close();
            }

        }
    }
}

//*[@id="page"]/div[4]/div[1]/p[2]