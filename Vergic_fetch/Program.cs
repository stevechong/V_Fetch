using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vergic_fetch
{
    class Program
    {
        static  void Main(string[] args)
        {
            List<Link> linkList = new List<Link>();


            Console.WriteLine("Loading Vergic site structure... ");
            getDirectories(linkList);
            Console.WriteLine("Site structure loaded... ");

            List<String> nodes = getNodes(linkList);

            Console.WriteLine("Please specify the folder path to save data to (e.g D:\\vergicsite) and then click Enter.. ");
            string path = Console.ReadLine();

            WritetoDisk(linkList, nodes, path);
        }

        private static List<String> getNodes(List<Link> linkList)
        {
            List<String> nodelist = new List<string>();
            foreach (Link l in linkList)
            {
                string node = string.Empty;
                if (l.HtmlLink.Contains("https://www.vergic.com/"))
                { 
                    node = l.HtmlLink.Replace("https://www.vergic.com/","");
                    int pos = node.IndexOf("/");
                    if (!String.IsNullOrEmpty(node.Trim()))
                    { 
                        node = node.Substring(0, pos);
                        if (!nodelist.Contains(node))
                        {
                          nodelist.Add(node);
                        }
                   }
               }
            }
            return nodelist;
        }

        private static void getDirectories(List<Link> linkList)
        {

            string rootname = string.Empty;

            string url = "https://vergic.com/";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex(GetDirectoryListingRegexForUrl(url));
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                Link l = new Link();
                                l.name = match.Groups["name"].ToString();
                                l.HtmlLink = getLink(match.ToString());

                                if (l.HtmlLink.TrimStart().ToLower().StartsWith("https") && (!String.IsNullOrEmpty(l.name) && !l.name.Substring(0,1).Equals("<")))
                                {
                                    linkList.Add(l);
                                }
                                
                            }
                        }
                    }
                }
            }
            
        }
     
        private static void WritetoDisk(List<Link> linkList, List<String> nodeList, string path)
        {
            bool folderExists = Directory.Exists(path);
            if (!folderExists)
            {
                Console.WriteLine("Folder doesn't exist...creating... ");
                try
                {
                    Directory.CreateDirectory(path);
                    Console.WriteLine("Folder created.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Folder not created...Exiting ");
                    Environment.Exit(0);
                }
            }
            else
            {
                Console.WriteLine("Folder exists already... ");
            }

            foreach (String node in nodeList)
            {
                if (linkList.Where(x => x.HtmlLink.Contains("/" + node.Trim() + "/")).Count() > 0)
                {
                    string subppath = path + "\\" + node.Trim();
                    folderExists = Directory.Exists(subppath);
                    if (!folderExists)
                    {
                        try
                        {
                            Directory.CreateDirectory(subppath);
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

          
            Parallel.ForEach(nodeList, item => NodeDownload(path, item, linkList));

            stopWatch.Stop();
            long duration = stopWatch.ElapsedMilliseconds;
            Console.WriteLine("........................................... ");
            Console.WriteLine("Download took " + duration.ToString() + " ms");
            Console.WriteLine("........................................... ");
            Console.WriteLine("Do you want to Exit the program [Y/N] ");
            string answer = Console.ReadLine().ToLower();

            if (answer.Equals("y"))
            {
                Environment.Exit(0);
            }


            //          foreach (String node in nodeList)
            //          {
            //              //Thread t = new Thread(() => NodeDownload(path, node, linkList));

            //              Thread t = new Thread(
            //() =>
            //{
            //    try
            //    {
            //        NodeDownload(path, node, linkList);
            //    }
            //    finally
            //    {
            //        //Console.WriteLine("Do you want to Exit the program [Y/N] ");
            //        //string answer = Console.ReadLine().ToLower();

            //        //if (answer.Equals("y"))
            //        //{
            //        //    Environment.Exit(0);
            //        //}
            //    }
            //});

            //              t.Start();
            //          }


        }

        private static void NodeDownload(String path, string node, List<Link> linkList)
        {
            WebClient client = new WebClient();

            string subppath = path + "\\" + node.Trim();

            if (linkList.Where(x => x.HtmlLink.Contains("/" + node.Trim() + "/")).Count() > 0)
            {
            foreach (Link l in linkList.Where(x => x.HtmlLink.Contains("/" + node.Trim() + "/")))
            {
                if (l.HtmlLink.StartsWith(" http"))
                {
                    try
                    {
                      //  Console.WriteLine("Saving " + l.name);
                        Uri url = new Uri(l.HtmlLink);

                        client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);
                        client.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                        client.DownloadFileAsync(url, @subppath + "\\" + l.name + ".html");

                        Console.WriteLine("Saved '" + l.name + "' to folder: " + @subppath);

                        while (client.IsBusy) { }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to save " + l.name);
                    }
                }
             }
            }
        }

        private static void Completed(object sender, AsyncCompletedEventArgs e)
        {
            //Console.WriteLine("Filed saved");
        }
       
        private static void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
           //RenderConsoleProgress(e.ProgressPercentage, '#', ConsoleColor.Green, "Downloading...");
        }

        private static string getRootName(string link)
        {
            link = link.Replace("https://www.vergic.com/", "");
            int pos = link.IndexOf("/");
            link = "https://www.vergic.com/" + link.Substring(0, pos - 1);
            return link;
        }

        private static string getLink(string linktext)
        {
            string temp = String.Empty;
            int pos = 0;
            linktext = linktext.Replace("<a href=", "");
            linktext = linktext.Replace(" </ a >", "");
            linktext = linktext.Replace((char)34, (char)32);
            linktext = linktext.Replace("target= _blank", "");
            linktext = linktext.Replace("class= link-to-post", "");
              
            pos = linktext.IndexOf(">");

            linktext = linktext.Substring(0, pos - 1);

            return linktext;

        }

        private static string GetDirectoryListingRegexForUrl(string url)
        {
            if (url.Equals("https://vergic.com/"))
            {
                return "<a href=\".*\">(?<name>.*)</a>";
            }
            throw new NotSupportedException();
        }

 
    }
}
