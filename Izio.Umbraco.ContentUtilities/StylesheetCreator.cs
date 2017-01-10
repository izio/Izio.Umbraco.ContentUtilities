using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Izio.Umbraco.ContentUtilities.Interfaces;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Izio.Umbraco.ContentUtilities
{
    public class StylesheetCreator : ICreator
    {
        private readonly IFileService _fileService;
        private readonly List<Stylesheet> _deployedStylesheets;

        public StylesheetCreator()
        {
            _fileService = ApplicationContext.Current.Services.FileService;
            _deployedStylesheets = new List<Stylesheet>();
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
                //get all stylesheet names
                var names = configuration.Descendants("Stylesheet").Select(a => a.Element("Name").Value);

                //check for conflicts
                if (CheckConflicts(names))
                {
                    //throw exception
                    throw new ArgumentException("The specified configuration could not be deployed as it contains stylesheets that already exist");
                }

                //get all stylesheets
                var stylesheetConfigurations = configuration.Descendants("Stylesheet");

                //create stylesheets
                foreach (var stylesheetConfiguration in stylesheetConfigurations)
                {
                    var stylesheet = CreateStylesheet(stylesheetConfiguration);

                    _deployedStylesheets.Add(stylesheet);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<TemplateCreator>("Failed to deploy stylesheets", ex);

                //delete deployed stylesheets
                foreach (var stylesheet in _deployedStylesheets)
                {
                    _fileService.DeleteStylesheet(stylesheet.Path);
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
                //get all stylesheet names
                var aliases = configuration.Descendants("Stylesheet").Select(a => a.Element("Name").Value);

                //delete all stylesheets
                foreach (var alias in aliases)
                {
                    _fileService.DeleteStylesheet(alias);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<TemplateCreator>("Failed to retract stylesheets", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        private bool CheckConflicts(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                //try and get stylesheet with the specified name
                var stylesheet = _fileService.GetStylesheetByName(name);

                //return true if stylesheet exists
                if (stylesheet != null)
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
        /// <param name="stylesheetConfiguration"></param>
        /// <returns></returns>
        private Stylesheet CreateStylesheet(XElement stylesheetConfiguration)
        {
            //create stylesheet
            var stylesheet = new Stylesheet(stylesheetConfiguration.Element("Name").Value)
            {
                Content = stylesheetConfiguration.Element("Content").Value
            };

            //save stylesheet
            _fileService.SaveStylesheet(stylesheet);

            return stylesheet;
        }
    }
}