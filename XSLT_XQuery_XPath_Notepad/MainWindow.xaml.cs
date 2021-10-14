using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ICSharpCode.AvalonEdit.Highlighting;
using Microsoft.Win32;
using Saxon.Api;
using Path = System.IO.Path;

namespace XSLT_XQuery_XPath_Notepad
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Processor processor;

        private XPathCompiler xpathCompiler;

        private XQueryCompiler xqueryCompiler;

        private XsltCompiler xsltCompiler;

        private DocumentBuilder docBuilder;

        private XPathSelector jsonBuilder;

        private Serializer serializer;
        public MainWindow()
        {
            InitializeComponent();

            processor = new Processor();

            xpathCompiler = processor.NewXPathCompiler();

            xqueryCompiler = processor.NewXQueryCompiler();

            xsltCompiler = processor.NewXsltCompiler();

            serializer = processor.NewSerializer();

            docBuilder = processor.NewDocumentBuilder();

            XPathCompiler jsonDocCompiler = processor.NewXPathCompiler();
            jsonDocCompiler.DeclareVariable(new QName("input"));

            jsonBuilder = jsonDocCompiler.Compile("parse-json($input)").Load();
            
        }

        private XdmItem ParseJson(string json)
        {
            jsonBuilder.SetVariable(new QName("input"), new XdmAtomicValue(json));
            return jsonBuilder.EvaluateSingle();
        }
        private void xpathEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            statusText.Text = "";
            resultEditor.Clear();

            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.SetOutputWriter(sw);

                    docBuilder.BaseUri = new Uri("urn:from-string");

                    var result = xpathCompiler.Evaluate(
                        codeEditor.Text,
                        (bool)xmlInputType.IsChecked ?
                        docBuilder.Build(new StringReader(inputEditor.Text))
                        : (bool)jsonInputType.IsChecked ?
                        ParseJson(inputEditor.Text) : null);

                    serializer.SerializeXdmValue(result);

                    resultEditor.Text = sw.ToString();
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
            }
        }

        private void xqueryEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            statusText.Text = "";
            resultEditor.Clear();

            List<XmlProcessingError> errorList = new List<XmlProcessingError>();
            xqueryCompiler.SetErrorList(errorList);

            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.SetOutputWriter(sw);

                    statusText.Text = "Compiling XQuery...";

                    var xqueryEvaluator = xqueryCompiler.Compile(codeEditor.Text).Load();

                    if ((bool)xmlInputType.IsChecked)
                    {
                        statusText.Text = "Parsing XML input document...";

                        docBuilder.BaseUri = new Uri("urn:from-string");
                        xqueryEvaluator.ContextItem = docBuilder.Build(new StringReader(inputEditor.Text));
                    }
                    else if ((bool)jsonInputType.IsChecked)
                    {
                        statusText.Text = "Parsing JSON input";

                        xqueryEvaluator.ContextItem = ParseJson(inputEditor.Text);
                    }

                    statusText.Text = "Running XQuery...";

                    xqueryEvaluator.Run(serializer);

                    statusText.Text = "";

                    resultEditor.Text = sw.ToString();
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
                if (errorList.Any())
                {
                    statusText.Text += string.Format(": {0}: {1}:{2}", errorList.First().Message, errorList.First().LineNumber, errorList.First().ColumnNumber);
                    resultEditor.Text = string.Join("\n", errorList.Select(error => string.Format("{0}: {1}:{2}", error.Message, error.LineNumber, error.ColumnNumber)));
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                }
            }
        }

        private void xsltTransformationButton_Click(object sender, RoutedEventArgs e)
        {
            statusText.Text = "";
            resultEditor.Clear();

            List<XmlProcessingError> errorList = new List<XmlProcessingError>();
            xsltCompiler.SetErrorList(errorList);

            try
            {
                statusText.Text = "Compiling XSLT code...";

                Xslt30Transformer transformer = xsltCompiler.Compile(new StringReader(codeEditor.Text)).Load30();

                XdmItem inputItem = null;

                if ((bool)xmlInputType.IsChecked)
                {
                    statusText.Text = "Parsing XML input document...";

                    docBuilder.BaseUri = new Uri("urn:from-string");
                    inputItem = docBuilder.Build(new StringReader(inputEditor.Text));
                }
                else if ((bool)jsonInputType.IsChecked)
                {
                    statusText.Text = "Parsing JSON input...";

                    inputItem = ParseJson(inputEditor.Text);
                }


                if (inputItem == null)
                {
                    using (StringWriter sw = new StringWriter())
                    {
                        serializer.SetOutputWriter(sw);

                        statusText.Text = "Running xsl:initialTemplate...";

                        transformer.CallTemplate(null, serializer);

                        statusText.Text = "";

                        resultEditor.Text = sw.ToString();
                        resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                    }
                }
                else
                {
                    using (StringWriter sw = new StringWriter())
                    {
                        serializer.SetOutputWriter(sw);

                        transformer.GlobalContextItem = inputItem;

                        statusText.Text = "Applying templates processing...";

                        transformer.ApplyTemplates(inputItem, serializer);

                        statusText.Text = "";

                        resultEditor.Text = sw.ToString();

                        resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
                    }
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
                if (errorList.Any())
                {
                    statusText.Text += string.Format(": {0}: {1}:{2}", errorList.First().Message, errorList.First().LineNumber, errorList.First().ColumnNumber);
                    resultEditor.Text = string.Join("\n", errorList.Select(error => string.Format("{0}: {1}:{2}", error.Message, error.LineNumber, error.ColumnNumber)));
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                }
            }
        }

        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void LoadXmlInput_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadFileIntoEditor(inputEditor, "XML files|*.xml|XHTML files|*.xhtml|All files|*.*");
        }

        private void LoadJsonInput_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadFileIntoEditor(inputEditor, "JSON files|*.json|All files|*.*");
        }

        private void LoadXsltCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadFileIntoEditor(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*");
        }

        private void LoadXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadFileIntoEditor(codeEditor, "XQuery files|*.xq;*.xquery|All files|*.*");
        }
        private void LoadXPathCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            LoadFileIntoEditor(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*");
        }
        private void SaveXsltCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*");
        }

        private void SaveXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XQuery files|*.xq;*.xquery|All files|*.*");
        }
        private void SaveXPathCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*");
        }
        private void SaveResultDocument_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(resultEditor, "HTML files|*.html;*.html|XML files|*.xml|Text files|*.txt;*.text|JSON files|*.json|All files|*.*");
        }

        private void SaveInputDocument_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveEditorToFile(inputEditor, "XML files|*.xml|JSON files|*.json|All files|*.*");
        }

        private void LoadFileIntoEditor(ICSharpCode.AvalonEdit.TextEditor editor, string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.CheckFileExists = true;

            openFileDialog.Filter = filter;

            if (openFileDialog.ShowDialog() ?? false)
            {
                //editor.Text = File.ReadAllText(openFileDialog.FileName);
                editor.Load(openFileDialog.FileName);
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(openFileDialog.FileName));
            }
        }

        private void SaveEditorToFile(ICSharpCode.AvalonEdit.TextEditor editor, string filter)
        {
            string currentFileName = null;

            if (currentFileName == null)
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.Filter = filter;

                if (saveFileDialog.ShowDialog() ?? false)
                {
                    currentFileName = saveFileDialog.FileName;
                }
                else
                {
                    return;
                }
            }

            editor.Save(currentFileName);
        }

        private void XmlInputType_Click(object sender, RoutedEventArgs e)
        {
            inputEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
        }

        private void JsonInputType_Click(object sender, RoutedEventArgs e)
        {
            inputEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Json");
        }
    }

}
