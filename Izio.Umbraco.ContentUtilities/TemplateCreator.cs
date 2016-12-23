using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Strings;

namespace Izio.Umbraco.ContentUtilities
{
    public class TemplateCreator
    {
        private readonly IFileService _fileService;
        private readonly List<Template> _deployedTemplates;

        public TemplateCreator()
        {
            _fileService = ApplicationContext.Current.Services.FileService;
            _deployedTemplates = new List<Template>();
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
            try
            {
                //get all templates
                var templates = configuration.Descendants("Template").Select(CreateTemplate);

                //save all templates
                foreach (var template in templates)
                {
                    if (CheckTemplateExists(template.Alias) == false)
                    {
                        _fileService.SaveTemplate(template);
                        _deployedTemplates.Add(template);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<TemplateCreator>("Failed to deply templates", ex);

                //delete deployed templates
                foreach (var template in _deployedTemplates)
                {
                    _fileService.DeleteTemplate(template.Alias.ToCleanString(CleanStringType.UnderscoreAlias));
                }
            }
        }

        private Template CreateTemplate(XElement templateConfiguration)
        {
            var template = new Template(templateConfiguration.Element("Name").Value, templateConfiguration.Element("Alias").Value)
            {
                Content = templateConfiguration.Element("Content").Value,
                MasterTemplateAlias = templateConfiguration.Element("MasterTemplateAlias").Value
            };

            return template;
        }

        private bool CheckTemplateExists(string alias)
        {
            var template = _fileService.GetTemplate(alias);

            return template != null;
        }
    }
}
