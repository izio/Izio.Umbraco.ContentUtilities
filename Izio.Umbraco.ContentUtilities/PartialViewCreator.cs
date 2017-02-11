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
    public class PartialViewCreator : ICreator
    {
        private readonly IFileService _fileService;
        private readonly List<IPartialView> _deployedPartialViews;

        public PartialViewCreator()
        {
            _fileService = ApplicationContext.Current.Services.FileService;
            _deployedPartialViews = new List<IPartialView>();
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
                //get all partial view paths
                var paths = configuration.Descendants("PartialView").Select(a => a.Element("Path").Value);

                //check for conflicts
                if (CheckConflicts(paths))
                {
                    //throw exception
                    throw new ArgumentException("The specified configuration could not be deployed as it contains partial views that already exist");
                }

                //get all partial views
                var partialViewConfigurations = configuration.Descendants("PartialView");

                //create partial views
                foreach (var partialViewConfiguration in partialViewConfigurations)
                {
                    var partialView = CreatePartialView(partialViewConfiguration);

                    _deployedPartialViews.Add(partialView);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<PartialViewCreator>("Failed to deply partial views", ex);

                //delete deployed partial views
                foreach (var partialView in _deployedPartialViews)
                {
                    _fileService.DeletePartialView(partialView.Alias);
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
                //get all partial view paths
                var paths = configuration.Descendants("PartialView").Select(a => a.Element("Path").Value);

                //delete all partial views
                foreach (var path in paths)
                {
                    _fileService.DeletePartialView(path);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<TemplateCreator>("Failed to retract partial views", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        private bool CheckConflicts(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                //try and get partial view with the specified path
                var partialView = _fileService.GetPartialView(path);

                //return true if partial view exists
                if (partialView != null)
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
        /// <param name="partialViewConfiguration"></param>
        /// <returns></returns>
        private IPartialView CreatePartialView(XElement partialViewConfiguration)
        {
            //create partial view
            var partialView = new PartialView(partialViewConfiguration.Element("Path").Value)
            {
                Content = partialViewConfiguration.Element("Content").Value
            };

            if (Convert.ToBoolean(partialViewConfiguration.Element("IsMacroPartial").Value))
            {
                //save macro partial view
                _fileService.SavePartialViewMacro(partialView);
            }
            else
            {
                //save partial view
                _fileService.SavePartialView(partialView);
            }

            return partialView;
        }
    }
}