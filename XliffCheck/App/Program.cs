using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace App
{
    class Program
    {
        static int _errorCount = 0;
        static int _warningCount = 0;
        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 1)
                {
                    Console.WriteLine($"##vso[task.complete result=Failed;] usage: XliffCheck <xliff directory>");
                    return;
                }
                var xliffDocuments = new Dictionary<string, XmlDocument>();
                XmlDocument xliffBaseFile = null;
                foreach (var file in Directory.EnumerateFiles(args[0]))
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(file);
                    if (file.EndsWith(".g.xlf"))
                    {
                        xliffBaseFile = xmlDoc;
                        continue;
                    }
                    var name = Path.GetFileNameWithoutExtension(file);
                    var key = name.Substring(name.Length - 5);
                    xliffDocuments.Add(key, xmlDoc);
                }
                var translations = new Dictionary<string, string>();
                if (xliffBaseFile != null)
                    AddTranslation(translations, xliffBaseFile);

                foreach (var xliffDoc in xliffDocuments)
                {
                    var nsmgr = new XmlNamespaceManager(xliffDoc.Value.NameTable);
                    nsmgr.AddNamespace("my", "urn:oasis:names:tc:xliff:document:1.2");
                    //Check aganst Project.g.xlf
                    if (xliffBaseFile != null)
                    {
                        for (int i = 0; i < translations.Count; i++)
                        {
                            var element = translations.ElementAt(i);
                            var node = xliffDoc.Value.SelectSingleNode($"//*[@id='{element.Key}']");
                            if (node == null)
                            {
                                Console.WriteLine($"##vso[task.logissue type=error] {xliffDoc.Key} Translation for '{element.Value}' is missing ({element.Key})");
                                _errorCount++;
                            }
                            else
                            {
                                var source = node.SelectSingleNode("my:source", nsmgr).InnerText;
                                if (!source.Equals(element.Value))
                                {
                                    Console.WriteLine($"##vso[task.logissue type=warning] Source '{element.Value}' (*.g.xlf) and '{source}' (*.{xliffDoc.Key}.xlf) are not the same ({element.Key})");
                                    _errorCount++;
                                }
                            }
                        }
                    }
                    //Check inside each xlf-File
                    foreach (XmlNode node in xliffDoc.Value.GetElementsByTagName("trans-unit"))
                    {
                        var targetElement = node.SelectSingleNode("my:target", nsmgr);
                        if (targetElement == null)
                            continue;
                        var sourceElement = node.SelectSingleNode("my:source", nsmgr);
                        if (CheckEntry("warning", targetElement.InnerText, "[NAB: REVIEW]", xliffDoc.Key, sourceElement.InnerText, node.Attributes["id"].Value, "needs review"))
                            continue;
                        if (CheckEntry("error", targetElement.InnerText, "[NAB: NOT TRANSLATED]", xliffDoc.Key, sourceElement.InnerText, node.Attributes["id"].Value, "is missing"))
                            continue;
                        if (CheckEntry("error", targetElement.InnerText, "", xliffDoc.Key, sourceElement.InnerText, node.Attributes["id"].Value, "is missing"))
                            continue;
                        if (targetElement.Attributes["state"] == null)
                            continue;
                        if (CheckEntry("warning", targetElement.Attributes["state"].Value, "needs-l10n", xliffDoc.Key, sourceElement.InnerText, node.Attributes["id"].Value, "needs adaptation"))
                            continue;
                        if (CheckEntry("warning", targetElement.Attributes["state"].Value, "needs-adaptation", xliffDoc.Key, sourceElement.InnerText, node.Attributes["id"].Value, "needs adaptation"))
                            continue;
                        if (CheckEntry("warning", targetElement.Attributes["state"].Value, "needs-review-translation", xliffDoc.Key, sourceElement.InnerText, node.Attributes["id"].Value, "needs review"))
                            continue;
                        if (CheckEntry("error", targetElement.Attributes["state"].Value, "new", xliffDoc.Key, sourceElement.InnerText, node.Attributes["id"].Value, "is missing"))
                            continue;
                    }
                }
                if (_errorCount == 0 && _warningCount == 0)
                {
                    Console.WriteLine($"##vso[task.complete result=Succeeded;]DONE");
                }
                else if (_errorCount == 0)
                {
                    Console.WriteLine($"##vso[task.complete result=SucceededWithIssues;]DONE");
                }
                else
                {
                    Console.WriteLine($"##vso[task.complete result=Failed;]DONE");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"##vso[task.logissue type=error]{ex.Message}");
                Console.WriteLine($"##vso[task.complete result=Failed;]DONE");
                return;
            }
        }

        private static bool CheckEntry(string logIssueType, string value, string valueToCheck, string lang, string sourceId, string sourceValue, string whatIsToDo)
        {
            if (value == valueToCheck || (valueToCheck == "[NAB: REVIEW]" && value.StartsWith(valueToCheck)))
            {
                Console.WriteLine($"##vso[task.logissue type={logIssueType}] {lang} Translation for '{sourceId}' {whatIsToDo} ({sourceValue})");
                switch(logIssueType)
                {
                    case "warning": _warningCount++; break;
                    case "error": _errorCount++; break;
                }
                return true;
            }
            return false;
        }

        static void AddTranslation(Dictionary<string, string> translations, XmlDocument xmlDoc)
        {
            foreach (XmlNode node in xmlDoc.ChildNodes[1].ChildNodes[0].ChildNodes[0].ChildNodes[0].ChildNodes)
            {

                var translate = node.Attributes["translate"].Value;
                var id = node.Attributes["id"].Value;
                if (translate == "yes")
                    translations.Add(id, node.ChildNodes[1].InnerText);
            }
        }
    }
}
