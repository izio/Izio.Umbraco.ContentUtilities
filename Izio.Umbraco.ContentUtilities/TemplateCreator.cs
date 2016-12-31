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
        private readonly List<ITemplate> _deployedTemplates;

        public TemplateCreator()
        {
            _fileService = ApplicationContext.Current.Services.FileService;
            _deployedTemplates = new List<ITemplate>();
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
                //get all content type aliases
                var aliases = configuration.Descendants("ContentType").Select(a => a.Element("Alias").Value);

                //check for conflicts
                if (CheckConflicts(aliases))
                {
                    //throw exception
                    throw new ArgumentException("The specified configuration could not be deployed as it contains content types that already exist");
                }

                //get all templates
                var templateConfigurations = configuration.Descendants("Template");

                //create templates
                foreach (var templateConfiguration in templateConfigurations)
                {
                    var template = CreateTemplate(templateConfiguration);

                    if (CheckTemplateExists(template.Alias))
                    {
                        _fileService.SaveTemplate(template);
                        _deployedTemplates.Add(template);
                    }
                }

                //update templates
                foreach (var templateConfiguration in templateConfigurations)
                {
                    var template = UpdateTemplate(templateConfiguration);

                    _fileService.SaveTemplate(template);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<TemplateCreator>("Failed to deply templates", ex);

                //delete deployed templates
                foreach (var template in _deployedTemplates)
                {
                    _fileService.DeleteTemplate(template.Alias);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private bool CheckConflicts(IEnumerable<string> aliases)
        {
            foreach (var alias in aliases)
            {
                //try and get template with the specified alias
                var template = _fileService.GetTemplate(alias.ToCleanString(CleanStringType.UnderscoreAlias));

                //return true if template exists
                if (template != null)
                {
                    return true;
                }
            }

            //no conflicts
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateConfiguration"></param>
        /// <returns></returns>
        private ITemplate CreateTemplate(XElement templateConfiguration)
        {
            var template = new Template(templateConfiguration.Element("Name").Value, templateConfiguration.Element("Alias").Value)
            {
                Content = templateConfiguration.Element("Content").Value
            };

            return template;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateConfiguration"></param>
        /// <returns></returns>
        private ITemplate UpdateTemplate(XElement templateConfiguration)
        {
            var template = _fileService.GetTemplate(templateConfiguration.Element("Alias").Value.ToCleanString(CleanStringType.UnderscoreAlias));
            var masterTemplate = _fileService.GetTemplate(templateConfiguration.Element("MasterTemplateAlias").Value.ToCleanString(CleanStringType.UnderscoreAlias));

            if (masterTemplate != null)
            {
                template.SetMasterTemplate(masterTemplate);
            }

            return template;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        private bool CheckTemplateExists(string alias)
        {
            var template = _fileService.GetTemplate(alias);

            return template == null;
        }
    }
}
