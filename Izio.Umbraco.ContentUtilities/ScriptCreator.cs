﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Izio.Umbraco.ContentUtilities
{
    public class ScriptCreator
    {
        private readonly IFileService _fileService;
        private readonly List<Script> _deployedScripts;

        public ScriptCreator()
        {
            _fileService = ApplicationContext.Current.Services.FileService;
            _deployedScripts = new List<Script>();
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
                //get all script names
                var names = configuration.Descendants("Script").Select(a => a.Element("Name").Value);

                //check for conflicts
                if (CheckConflicts(names))
                {
                    //throw exception
                    throw new ArgumentException("The specified configuration could not be deployed as it contains scripts that already exist");
                }

                //get all scripts
                var scriptConfigurations = configuration.Descendants("Script");

                //create scripts
                foreach (var scriptConfiguration in scriptConfigurations)
                {
                    var script = CreateScript(scriptConfiguration);

                    _deployedScripts.Add(script);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<TemplateCreator>("Failed to deply scripts", ex);

                //delete deployed scripts
                foreach (var script in _deployedScripts)
                {
                    _fileService.DeleteScript(script.Path);
                }
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
                //try and get script with the specified name
                var script = _fileService.GetScriptByName(name);

                //return true if script exists
                if (script != null)
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
        /// <param name="scriptConfiguration"></param>
        /// <returns></returns>
        private Script CreateScript(XElement scriptConfiguration)
        {
            //create script
            var script = new Script(scriptConfiguration.Element("Name").Value)
            {
                Content = scriptConfiguration.Element("Content").Value
            };

            //save script
            _fileService.SaveScript(script);

            return script;
        }
    }
}