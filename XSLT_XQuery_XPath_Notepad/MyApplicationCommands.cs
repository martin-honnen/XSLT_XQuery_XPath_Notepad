using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace XSLT_XQuery_XPath_Notepad.Commands
{
    public class MyApplicationCommands
    {
        public static RoutedUICommand LoadXmlInput = new RoutedUICommand("Load XML input file", "LoadXmlInput", typeof(MyApplicationCommands));

        public static RoutedUICommand LoadJsonInput = new RoutedUICommand("Load JSON input file", "LoadJsonInput", typeof(MyApplicationCommands));

        public static RoutedUICommand LoadXsltCode = new RoutedUICommand("Load XSLT file", "LoadXsltCode", typeof(MyApplicationCommands));

        public static RoutedUICommand LoadXQueryCode = new RoutedUICommand("Load XQuery file", "LoadXQueryCode", typeof(MyApplicationCommands));

        public static RoutedUICommand SaveInputDocument = new RoutedUICommand("Save input document", "SaveInputDocument", typeof(MyApplicationCommands));

        public static RoutedUICommand SaveXsltCode = new RoutedUICommand("Save XSLT code", "SaveXsltCode", typeof(MyApplicationCommands));

        public static RoutedUICommand SaveXQueryCode = new RoutedUICommand("Save XQuery code", "SaveXQueryCode", typeof(MyApplicationCommands));

        public static RoutedUICommand SaveResultDocument = new RoutedUICommand("Save result document", "SaveResultDocument", typeof(MyApplicationCommands));
    }
}
