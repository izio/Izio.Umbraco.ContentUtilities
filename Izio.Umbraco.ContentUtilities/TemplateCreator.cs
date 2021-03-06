﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Izio.Umbraco.ContentUtilities.Interfaces;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Core.Strings;

namespace Izio.Umbraco.ContentUtilities
{
    /// <summary>
    /// 
    /// </summary>
    public class TemplateCreator : ICreator
    {
        private readonly IFileService _fileService;
        private readonly List<ITemplate> _deployedTemplates;

        /// <summary>
        /// 
        /// </summary>
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
                //get all template aliases
                var aliases = configuration.Descendants("Template").Select(a => a.Element("Alias").Value);

                //check for conflicts
                if (CheckConflicts(aliases))
                {
                    //throw exception
                    throw new ArgumentException("The specified configuration could not be deployed as it contains templates that already exist");
                }

                //get all templates
                var templateConfigurations = configuration.Descendants("Template");

                //create templates
                foreach (var templateConfiguration in templateConfigurations)
                {
                    var template = CreateTemplate(templateConfiguration);

                    _deployedTemplates.Add(template);
                }

                //update templates
                foreach (var templateConfiguration in templateConfigurations)
                {
                    UpdateTemplate(templateConfiguration);
                }
            }
            catch (Exception ex)
            {
                //log exception
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
        /// <param name="path"></param>
        public void Retract(string path)
        {
            var document = XDocument.Load(path);

            Retract(document);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public void Retract(XDocument configuration)
        {
            try
            {
                //get all template aliases
                var aliases = configuration.Descendants("Template").Select(a => a.Element("Alias").Value);

                //delete all templates
                foreach (var alias in aliases)
                {
                    _fileService.DeleteTemplate(alias.ToCleanString(CleanStringType.UnderscoreAlias));
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<TemplateCreator>("Failed to retract templates", ex);
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
                var template = _fileService.GetTemplate(alias.ToSafeAlias());

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
            //create template
            var template = new Template(templateConfiguration.Element("Name").Value, templateConfiguration.Element("Alias").Value.ToSafeAlias())
            {
                Content = templateConfiguration.Element("Content").Value
            };

            //save template
            _fileService.SaveTemplate(template);

            return template;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="templateConfiguration"></param>
        /// <returns></returns>
        private ITemplate UpdateTemplate(XElement templateConfiguration)
        {
            //get template
            var template = _fileService.GetTemplate(templateConfiguration.Element("Alias").Value.ToSafeAlias());

            return UpdateTemplate(template, templateConfiguration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <param name="templateConfiguration"></param>
        /// <returns></returns>
        private ITemplate UpdateTemplate(ITemplate template, XElement templateConfiguration)
        {
            //check if a master template has been specified
            if (string.IsNullOrEmpty(templateConfiguration.Element("MasterTemplateAlias").Value) == false)
            {
                //get master template
                var masterTemplate = _fileService.GetTemplate(templateConfiguration.Element("MasterTemplateAlias").Value.ToSafeAlias());

                //check template exists
                if (masterTemplate != null)
                {
                    //set master template
                    template.SetMasterTemplate(masterTemplate);
                }

                //save the template
                _fileService.SaveTemplate(template);
            }

            return template;
        }
    }
}