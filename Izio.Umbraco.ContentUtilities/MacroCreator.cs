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
    /// <summary>
    /// 
    /// </summary>
    public class MacroCreator : ICreator
    {
        private readonly IMacroService _macroService;
        private readonly List<IMacro> _deployedMacros;
        private readonly IEnumerable<IDataTypeDefinition> _dataTypeDefinitions;

        /// <summary>
        /// 
        /// </summary>
        public MacroCreator()
        {
            _macroService = ApplicationContext.Current.Services.MacroService;
            _deployedMacros = new List<IMacro>();
            _dataTypeDefinitions = ApplicationContext.Current.Services.DataTypeService.GetAllDataTypeDefinitions();
        }

        #region ICreator

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
                //get all macro aliases
                var aliases = configuration.Descendants("Macro").Select(a => a.Element("Alias").Value);

                //check for conflicts
                if (CheckConflicts(aliases))
                {
                    //throw exception
                    throw new ArgumentException("The specified configuration could not be deployed as it contains macros that already exist");
                }

                //get all macros
                var macroConfigurations = configuration.Descendants("Macro");

                //create macros
                foreach (var macroConfiguration in macroConfigurations)
                {
                    var macro = CreateMacro(macroConfiguration);

                    _deployedMacros.Add(macro);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<MacroCreator>("Failed to deply macros", ex);

                //delete deployed macros
                foreach (var macro in _deployedMacros)
                {
                    _macroService.Delete(macro);
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
                //get all macro aliases
                var aliases = configuration.Descendants("Macro").Select(a => a.Element("Alias").Value);

                //delete all macros
                foreach (var alias in aliases)
                {
                    var macro = _macroService.GetByAlias(alias.ToSafeAlias());

                    _macroService.Delete(macro);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<MacroCreator>("Failed to retract macros", ex);
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private bool CheckConflicts(IEnumerable<string> aliases)
        {
            foreach (var alias in aliases)
            {
                //try and get macro with the specified alias
                var macro = _macroService.GetByAlias(alias.ToSafeAlias());

                //return true if macro exists
                if (macro != null)
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
        /// <param name="macroConfiguration"></param>
        /// <returns></returns>
        private IMacro CreateMacro(XElement macroConfiguration)
        {
            //create macro
            var macro = new Macro
            {
                Name = macroConfiguration.Element("Name").Value,
                Alias = macroConfiguration.Element("Alias").Value.ToSafeAlias(),
                ScriptPath = macroConfiguration.Element("ScriptPath") == null ? "" : macroConfiguration.Element("ScriptPath").Value,
                XsltPath = macroConfiguration.Element("XsltPath") == null ? "" : macroConfiguration.Element("XsltPath").Value,
                ControlType = macroConfiguration.Element("ControlType") == null ? "" : macroConfiguration.Element("ControlType").Value,
                ControlAssembly = macroConfiguration.Element("ControlAssembly") == null ? "" : macroConfiguration.Element("ControlAssembly").Value,
                UseInEditor = Convert.ToBoolean(macroConfiguration.Element("UseInEditor").Value),
                CacheByMember = Convert.ToBoolean(macroConfiguration.Element("CacheByMember").Value),
                CacheByPage = Convert.ToBoolean(macroConfiguration.Element("CacheByPage").Value),
                CacheDuration = Convert.ToInt32(macroConfiguration.Element("CacheDuration").Value)
            };

            //get macro properties
            var properties = GetMacroPropertyCollection(macroConfiguration.Descendants("Property"));

            //add properties to macro
            foreach (var property in properties)
            {
                macro.Properties.Add(property);
            }

            //save macro
            _macroService.Save(macro);

            return macro;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private MacroPropertyCollection GetMacroPropertyCollection(IEnumerable<XElement> properties)
        {
            //create macro property list
            var macroProperties = new MacroPropertyCollection();

            foreach (var property in properties)
            {
                //create new property
                var macroProperty = new MacroProperty()
                {
                    Name = property.Element("Name").Value,
                    Alias = property.Element("Alias").Value.ToSafeAlias(),
                    EditorAlias = _dataTypeDefinitions.FirstOrDefault(t => property.Element("Type") != null && (t.Name.ToLower() == property.Element("Type").Value.ToLower() || t.PropertyEditorAlias.ToLower() == property.Element("Type").Value.ToLower())).PropertyEditorAlias,
                    SortOrder = macroProperties.Count
                };

                //add property to collection
                macroProperties.Add(macroProperty);
            }

            return macroProperties;
        }
    }
}