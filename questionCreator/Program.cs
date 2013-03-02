using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;


namespace questionCreator
{
    class Program
    {
        private static int MINIMAL_QUESTION_LENGTH = 100;
        private static int MAXIMAL_QUESTION_LENGTH = 150;
        private static string HEBREW_WIKI_LINK = "http://he.wikipedia.org/wiki/";
        static void Main(string[] args)
        {
            Database db = new Database();
            db.getRandomQuestion(); 
            db.shutdown();
        }


        /**
         * USAGE : 
         * var categoryToDownload = "http://he.wikipedia.org/wiki/%D7%A7%D7%98%D7%92%D7%95%D7%A8%D7%99%D7%94:%D7%90%D7%AA%D7%A8%D7%99_%D7%94%D7%9E%D7%A7%D7%A8%D7%90";
         *   makeQuestionsFromCategory(db, categoryToDownload);
         */
        private static void makeQuestionsFromCategory(Database db, string categoryToDownload)
        {
            var pathToPage = downloadAndSaveWikiPage(categoryToDownload);

            HtmlDocument doc = new HtmlDocument();
            doc.Load(pathToPage);

            // subPages
            var pages = doc.GetElementbyId("mw-pages").Descendants("li");
            foreach (var page in pages)
            {
                var linkToPage = HEBREW_WIKI_LINK + page.InnerText;
                var wrongAnswers = generateWrongAnswers(pages.ToArray(), page);
                downloadMakeQuestionAndSave(db, linkToPage, wrongAnswers);
                Debug.WriteLine("downloaded {0}", page.InnerText);
            }

        }

        private static string[] generateWrongAnswers(HtmlNode[] pagesArray, HtmlNode page)
        {
            var randomGenerator = new Random();
            var wrongAnswers = new String[3];
            var wrongAnswerCount = 0;
            while (wrongAnswerCount < 3)
            {
                var currentWrongAnswer = pagesArray[randomGenerator.Next(0, pagesArray.Length)].InnerText;
                if (!wrongAnswers.Contains(currentWrongAnswer) && !currentWrongAnswer.Equals(page.InnerText))
                {
                    wrongAnswers[wrongAnswerCount++] = currentWrongAnswer;
                }
            }
            return wrongAnswers;
        }

        private static void downloadMakeQuestionAndSave(Database db, string pageToDownload, string[] wrongAnswers)
        {
            var pathToPage = downloadAndSaveWikiPage(pageToDownload);

            HtmlDocument doc = new HtmlDocument();
            doc.Load(pathToPage);

            // NAME
            var wikiValueName = doc.GetElementbyId("firstHeading").SelectNodes("span")[0].InnerText;
            var link = @"http://he.wikipedia.org/wiki/" + wikiValueName;
            wikiValueName = removeParenthasis(wikiValueName);

            // Paragraphs
            var mainContent = doc.GetElementbyId("mw-content-text");
            var paragraphs = mainContent.SelectNodes("p");
            var i = 1;
            string paragraph = makeQuestion(paragraphs.First<HtmlNode>().InnerText, wikiValueName);
            if (paragraph.Equals("האם התכוונתם ל..."))
            {
                Debug.WriteLine("doesn't exist...");
                db.shutdown();
                return;
            }
            while (paragraph.Length < MINIMAL_QUESTION_LENGTH && i < paragraphs.Count)
            {
                paragraph += " " + makeQuestion(paragraphs[i].InnerText, wikiValueName);
                i++;
            }

            while (paragraph.Length > MAXIMAL_QUESTION_LENGTH)
            {
                var oneBeforeLast = paragraph.Substring(0, paragraph.Length - 1).LastIndexOf(".");
                if (oneBeforeLast >= 100)
                {
                    paragraph = paragraph.Substring(0, oneBeforeLast + 1);
                }
                else
                {
                    break;
                }
            }


            Debug.WriteLine(paragraph);

            // CATS
            var categoriesContainer = doc.GetElementbyId("mw-normal-catlinks");
            var categoriesList = categoriesContainer.SelectNodes("ul/li/a");

            foreach (var item in categoriesList)
            {
                var catName = item.InnerText;
                var catLink = item.Attributes["href"].Value;
            }


            db.addQuestion(paragraph, wikiValueName, wrongAnswers, link);
            return;
        }

        private static void downloadAndSaveRunWikiPages(int numPages)
        {
            var filePath = "http://he.wikipedia.org/wiki/Special:Random";


            for (int i = 0; i < numPages; i++)
            {
                try
                {
                    downloadAndSaveWikiPage(filePath);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Due to exception, skipped this line. %s", e.Message);
                    continue;
                }

            }
        }

        private static string getTextBetween(string text, string start, string end) {
            var stage1 = text.Substring(text.IndexOf(start) + start.Length + 1); 
            var stage2 = stage1.Substring(0, stage1.IndexOf(end));
            return stage2;
        }

        private static string stripForbiddenFileNames(string text)
        {
            Regex rgx = new Regex("[:/\\*?\"<>|]");
            return rgx.Replace(text, " ");
        }

        private static string makeQuestion(string html, string answer)
        {
            //var words = answer.Split(' ');
            //var i = 1;
            //foreach (var word in words)
            //{
            //    Regex rgx = new Regex(".?" + word+ ".?");
            //    html = rgx.Replace(html, i.ToString() + "______");
            //    i++;
            //}
            var replacerPattern = "____";
            var replacer = "";
            foreach (var item in answer.Split(' '))
            {
                replacer += replacerPattern + " ";
            }

            Regex rgx = new Regex(".?" + answer + ".?");
            html = rgx.Replace(html, replacer);


           return removeParenthasis(html);
            
        }

        private static string removeParenthasis(string text)
        {
            Regex rgx = new Regex("(\\(.*\\))");
            return rgx.Replace(text, "");
        }

        private static string downloadAndSaveWikiPage(string filePath)
        {
            WebClient client;
            Encoding enc = Encoding.GetEncoding("UTF-8");

            string folderPath = @"c:\downloadedFiles\", fileName;
            byte[] by;
            string text;
            String fileNameStart = "<span dir=\"auto\">",
                fileNameEnd = "</span>";
            string firstHeading = "\"firstHeading\"";

            client = new WebClient();
            client.Headers.Set("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:12.0) Gecko/20100101 Firefox/12.0");
            client.Encoding = enc;
            by = client.DownloadData(filePath);
            text = enc.GetString(by);
            fileName = getTextBetween(text, firstHeading, fileNameEnd);
            fileName = fileName.Substring(fileName.IndexOf(fileNameStart) + fileNameStart.Length);
            fileName = stripForbiddenFileNames(fileName);
            var path = folderPath + fileName + ".html";
            System.IO.File.WriteAllText(path, text, enc);
            return path;
        }
    }
}
