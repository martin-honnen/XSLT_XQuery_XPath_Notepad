using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
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
        private static readonly string defaultBaseInputURI = "urn:from-string";

        private string baseXsltCodeURI = defaultBaseInputURI;

        private string baseXQueryCodeURI = defaultBaseInputURI;

        private string baseXPathCodeURI = defaultBaseInputURI;

        private static Processor processor = new Processor();

        private XPathCompiler xpathCompiler;

        private XQueryCompiler xqueryCompiler;

        private XsltCompiler xsltCompiler;

        private DocumentBuilder docBuilder;

        private XPathSelector jsonBuilder;

        private Serializer serializer;

        private XPathSelector xpathResultSerializer;

        private SelectionChangedEventHandler selectionChangedEventHandler;

        private DispatcherTimer typingTimer;
        public MainWindow()
        {
            InitializeComponent();

            xpathCompiler = processor.NewXPathCompiler();

            xqueryCompiler = processor.NewXQueryCompiler();

            xsltCompiler = processor.NewXsltCompiler();

            serializer = processor.NewSerializer();

            docBuilder = processor.NewDocumentBuilder();

            XPathCompiler jsonDocCompiler = processor.NewXPathCompiler();
            jsonDocCompiler.DeclareVariable(new QName("input"));

            jsonBuilder = jsonDocCompiler.Compile("parse-json($input)").Load();

            XPathCompiler xpathResultCompiler = processor.NewXPathCompiler();
            xpathResultCompiler.DeclareVariable(new QName("value"));
            xpathResultCompiler.DeclareVariable(new QName("serialization-parameters"));

            xpathResultSerializer = xpathResultCompiler.Compile("serialize($value, $serialization-parameters)").Load();

            typingTimer = new DispatcherTimer(DispatcherPriority.ContextIdle);
            typingTimer.Interval = TimeSpan.FromSeconds(1.2);
            typingTimer.Tick += TypingTimer_Tick;

            typingTimer.IsEnabled = (bool)autoEvaluateCbx.IsChecked;
            
        }

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            if ((bool)autoEvaluateCbx.IsChecked)
            {
                typingTimer.IsEnabled = false;
                typingTimer.Stop();
                evaluateCurrentCodeType();
            }
        }

        private XdmItem ParseJson(string json)
        {
            jsonBuilder.SetVariable(new QName("input"), new XdmAtomicValue(json));
            return jsonBuilder.EvaluateSingle();
        }
        private void xpathEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            runXPathEvaluation();
        }

        private void xqueryEvaluationBtn_Click(object sender, RoutedEventArgs e)
        {
            runXQueryEvaluation();
        }

        private void xsltTransformationButton_Click(object sender, RoutedEventArgs e)
        {
            runXsltTransformation();
        }

        private void DisplayResultDocuments(Dictionary<string, string> serializedDocuments)
        {
            resultDocumentList.ItemsSource = serializedDocuments.Keys;
            resultDocumentList.SelectionChanged += selectionChangedEventHandler = (s, e) => DisplayResultDocuments(serializedDocuments[((ComboBox)s).SelectedItem as string]);
            resultDocumentList.SelectedIndex = 0;
        }

        private void DisplayResultDocuments(string result)
        {
            resultEditor.Text = result;

            resultWebView.NavigateToString(result);

            resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
        }

        private void ClearResultDocumentList()
        {
            if (selectionChangedEventHandler != null)
            {
                resultDocumentList.SelectionChanged -= selectionChangedEventHandler;
            }
            resultDocumentList.ItemsSource = null;
        }

        private void ShowResultDocumentList()
        {
            resultPanel.Visibility = Visibility.Visible;
        }

        private void HideResultDocumentList()
        {
            resultPanel.Visibility = Visibility.Collapsed;
        }
        private void ShowGridRow(int rowIndex)
        {
            mainGrid.RowDefinitions[rowIndex].Height = new GridLength(1, GridUnitType.Star);
        }
        private void HideGridRow(int rowIndex)
        {
            mainGrid.RowDefinitions[rowIndex].Height = new GridLength(0);
        }
        private void CommonCommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void NewPadWindow_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var secondaryWindow = new MainWindow();
            secondaryWindow.Show();
        }
        private void NewXsltCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            codeEditor.Text = @"<xsl:stylesheet xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" version=""3.0""
  xmlns:xs=""http://www.w3.org/2001/XMLSchema""
  exclude-result-prefixes=""#all""
  expand-text=""yes"">

  <xsl:mode on-no-match=""shallow-copy""/>

  <xsl:output indent=""yes""/>

  <xsl:template match=""/"">
    <xsl:copy>
      <xsl:apply-templates/>
      <xsl:comment>Run with {system-property('xsl:product-name')} {system-property('xsl:product-version')} at {current-dateTime()}</xsl:comment>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>";

        }

        private void NewXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            codeEditor.Text = @"declare namespace map = ""http://www.w3.org/2005/xpath-functions/map"";
declare namespace array = ""http://www.w3.org/2005/xpath-functions/array"";

declare namespace output = ""http://www.w3.org/2010/xslt-xquery-serialization"";

declare option output:method ""xml"";
declare option output:indent ""yes"";

.";

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
            baseXsltCodeURI = LoadFileIntoEditor(codeEditor, "XSLT files|*.xsl;*.xslt|All files|*.*") ?? defaultBaseInputURI;
        }

        private void LoadXQueryCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseXQueryCodeURI = LoadFileIntoEditor(codeEditor, "XQuery files|*.xq;*.xquery|All files|*.*");
        }
        private void LoadXPathCode_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            baseXPathCodeURI = LoadFileIntoEditor(codeEditor, "XPath files|*.xpath;*.xp|All files|*.*");
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

        private string LoadFileIntoEditor(ICSharpCode.AvalonEdit.TextEditor editor, string filter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            openFileDialog.CheckFileExists = true;

            openFileDialog.Filter = filter;

            if (openFileDialog.ShowDialog() ?? false)
            {
                //editor.Text = File.ReadAllText(openFileDialog.FileName);
                editor.Load(openFileDialog.FileName);
                editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinitionByExtension(Path.GetExtension(openFileDialog.FileName));
                return openFileDialog.FileName;
            }

            return null;
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

        private void runXsltTransformation()
        {
            statusText.Text = "";
            ClearResultDocumentList();
            ShowResultDocumentList();
            resultEditor.Clear();

            List<XmlProcessingError> errorList = new List<XmlProcessingError>();
            xsltCompiler.SetErrorList(errorList);

            xsltCompiler.BaseUri = new Uri(baseXsltCodeURI);

            try
            {
                statusText.Text = "Compiling XSLT code...";

                Xslt30Transformer transformer = xsltCompiler.Compile(new StringReader(codeEditor.Text)).Load30();

                transformer.BaseOutputURI = "urn:to-string";

                var mainSerializer = new MySerializer(processor);
                Dictionary<string, MySerializer> resultDocuments = new Dictionary<string, MySerializer>();
                resultDocuments["*** principal result ***"] = mainSerializer;

                transformer.ResultDocumentHandler = new MyResultDocumentsHandler(processor, resultDocuments);

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
                    statusText.Text = "Running xsl:initialTemplate...";

                    transformer.CallTemplate(null, mainSerializer.serializer);

                    statusText.Text = "";

                    DisplayResultDocuments((transformer.ResultDocumentHandler as MyResultDocumentsHandler).GetSerializedResultDocuments());
                }
                else
                {
                    transformer.GlobalContextItem = inputItem;

                    statusText.Text = "Applying templates processing...";

                    transformer.ApplyTemplates(inputItem, mainSerializer.serializer);

                    statusText.Text = "";

                    DisplayResultDocuments((transformer.ResultDocumentHandler as MyResultDocumentsHandler).GetSerializedResultDocuments());
                }
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
                //throw ex;
                if (errorList.Any())
                {
                    statusText.Text += string.Format(": {0}: {1}:{2}", errorList.First().Message, errorList.First().LineNumber, errorList.First().ColumnNumber);
                    resultEditor.Text = string.Join("\n", errorList.Select(error => string.Format("{0}: {1}:{2}", error.Message, error.LineNumber, error.ColumnNumber)));
                    resultEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("Text");
                }
            }
        }

        private void codeEditor_TextChanged(object sender, EventArgs e)
        {
            if ((bool)autoEvaluateCbx.IsChecked)
            {
                typingTimer.Start();
            }
        }

        private void autoEvaluateCbx_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)autoEvaluateCbx.IsChecked)
            {
                typingTimer.IsEnabled = true;
                typingTimer.Start();
            }
            else
            {
                typingTimer.IsEnabled = false;
                typingTimer.Stop();
            }
        }

        private void evaluateCode_Click(object sender, RoutedEventArgs e)
        {
            evaluateCurrentCodeType();
        }

        private void evaluateCurrentCodeType()
        {
            if ((bool)codeTypeXslt.IsChecked)
            {
                runXsltTransformation();
            }
            else if ((bool)codeTypeXQuery.IsChecked)
            {
                runXQueryEvaluation();
            }
            else if ((bool)codeTypeXPath.IsChecked)
            {
                runXPathEvaluation();
            }
        }

        private void runXPathEvaluation()
        {
            statusText.Text = "";
            HideResultDocumentList();
            ClearResultDocumentList();
            resultEditor.Clear();

            try
            {
                var xpathCode = codeEditor.Text;

                var serializationParamsCode = Regex.Replace(xpathCode, @"(^\(:(?!:\))(.+?):\))?(.*)", "$2", RegexOptions.Singleline);

                if (serializationParamsCode == "")
                {
                    serializationParamsCode = "map{}";
                }

                var serializationParams = xpathCompiler.EvaluateSingle(serializationParamsCode, null);

                //serializer.SetOutputWriter(sw);

                docBuilder.BaseUri = new Uri("urn:from-string");

                xpathCompiler.BaseUri = new Uri(baseXPathCodeURI).AbsoluteUri;

                var result = xpathCompiler.Evaluate(
                    codeEditor.Text,
                    (bool)xmlInputType.IsChecked ?
                    docBuilder.Build(new StringReader(inputEditor.Text))
                    : (bool)jsonInputType.IsChecked ?
                    ParseJson(inputEditor.Text) : null);

                //serializer.SerializeXdmValue(result);

                xpathResultSerializer.SetVariable(new QName("value"), result);
                xpathResultSerializer.SetVariable(new QName("serialization-parameters"), serializationParams);

                var resultString = xpathResultSerializer.EvaluateSingle().GetStringValue();

                resultEditor.Text = resultString;
            }
            catch (Exception ex)
            {
                statusText.Text = ex.Message;
            }
        }

        private void runXQueryEvaluation()
        {
            statusText.Text = "";
            ClearResultDocumentList();
            HideResultDocumentList();
            resultEditor.Clear();

            List<XmlProcessingError> errorList = new List<XmlProcessingError>();
            xqueryCompiler.SetErrorList(errorList);
            xqueryCompiler.BaseUri = new Uri(baseXQueryCodeURI).AbsoluteUri;

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

                    var result = sw.ToString();
                    resultEditor.Text = result;
                    resultWebView.NavigateToString(result);
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

        private void codeTypeXslt_Click(object sender, RoutedEventArgs e)
        {

        }

        private void codeTypeXQuery_Click(object sender, RoutedEventArgs e)
        {

        }

        private void codeTypeXPath_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}
