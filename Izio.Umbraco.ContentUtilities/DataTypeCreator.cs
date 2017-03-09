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
    public class DataTypeCreator : ICreator
    {
        private readonly IDataTypeService _dataTypeService;
        private readonly List<DataTypeDefinition> _deployedDataTypes;

        /// <summary>
        /// 
        /// </summary>
        public DataTypeCreator()
        {
            _dataTypeService = ApplicationContext.Current.Services.DataTypeService;
            _deployedDataTypes = new List<DataTypeDefinition>();
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
                //get all data type names
                var names = configuration.Descendants("DataType").Select(a => a.Element("Name").Value);

                //check for conflicts
                if (CheckConflicts(names))
                {
                    //throw exception
                    throw new ArgumentException("The specified configuration could not be deployed as it contains data types that already exist");
                }

                //get all data type configurations
                var dataTypeConfigurations = configuration.Descendants("DataType");

                //create data types
                foreach (var contentTypeConfiguration in dataTypeConfigurations)
                {
                    var dataType = CreateDataType(contentTypeConfiguration);

                    _deployedDataTypes.Add(dataType);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<DataTypeCreator>("Failed to deply data types", ex);

                //delete deployed data types
                foreach (var dataType in _deployedDataTypes)
                {
                    _dataTypeService.Delete(dataType);
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
                //get all data type names
                var names = configuration.Descendants("DataType").Select(a => a.Element("Name").Value);

                //delete all data types
                foreach (var name in names)
                {
                    var dataType = _dataTypeService.GetDataTypeDefinitionByName(name);

                    _dataTypeService.Delete(dataType);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<DataTypeCreator>("Failed to retract data types", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        private bool CheckConflicts(IEnumerable<string> names)
        {
            //get existing data type names
            var existingDataTypeNames = _dataTypeService.GetAllDataTypeDefinitions().Select(d => d.Name);

            //check whether there are any clashes with existing content type aliases
            if (names.Intersect(existingDataTypeNames).Any())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTypeConfiguration"></param>
        /// <returns></returns>
        private DataTypeDefinition CreateDataType(XElement dataTypeConfiguration)
        {
            //create datatype
            var dataTypeDefinition = new DataTypeDefinition(dataTypeConfiguration.Element("Type").Value)
            {
                Name = dataTypeConfiguration.Element("Name").Value,
                DatabaseType = dataTypeConfiguration.Element("DatabaseType").Value.EnumParse<DataTypeDatabaseType>(true)
            };

            //create prevalues
            var preValues = dataTypeConfiguration.Descendants("PreValue").ToDictionary(key => key.Element("Alias").Value, value => new PreValue(value.Element("Value").Value.ToString()));
            
            //save data type and prevalues
            _dataTypeService.SaveDataTypeAndPreValues(dataTypeDefinition, preValues);

            return dataTypeDefinition;
        }
    }
}