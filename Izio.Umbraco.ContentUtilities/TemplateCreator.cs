using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Izio.Umbraco.ContentUtilities
{
    public class TemplateCreator
    {
        private readonly IFileService _fileService;

        public TemplateCreator()
        {
            _fileService = ApplicationContext.Current.Services.FileService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        public void Deploy(string path)
        {
            var document = XDocument.Load(path);

            Deploy(document);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public void Deploy(XDocument configuration)
        {
            //get all templates
            var templates = configuration.Descendants("Template").Select(CreateTemplate);

            //save all templates
            foreach (var template in templates)
            {
                _fileService.SaveTemplate(template);
            }
        }

        public Template CreateTemplate(XElement templateConfiguration)
        {
            var template = new Template(templateConfiguration.Element("Name").Value, templateConfiguration.Element("Alias").Value)
            {
                Content = templateConfiguration.Element("Content").Value,
                MasterTemplateAlias = templateConfiguration.Element("MaterTemplateAlias").Value
            };

            return template;
        }
    }
}
