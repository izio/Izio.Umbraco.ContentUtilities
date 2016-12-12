using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Izio.Umbraco.ContentUtilities
{
    /// <summary>
    /// 
    /// </summary>
    public static class ContentTypeCreator
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public static void Deploy(string path)
        {
            var document = XDocument.Load("path");

            Deploy(document);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public static void Deploy(XDocument configuration)
        {
            
        }
    }
}