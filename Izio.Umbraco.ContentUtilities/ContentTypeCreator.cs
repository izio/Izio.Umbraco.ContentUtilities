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
    /// <summary>
    /// 
    /// </summary>
    public class ContentTypeCreator
    {
        private readonly IContentTypeService _contentTypeService;
        private readonly IFileService _fileService;
        private readonly IEnumerable<IDataTypeDefinition> _dataTypeDefinitions;

        /// <summary>
        /// 
        /// </summary>
        public ContentTypeCreator()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _fileService = ApplicationContext.Current.Services.FileService;
            _dataTypeDefinitions = ApplicationContext.Current.Services.DataTypeService.GetAllDataTypeDefinitions();
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
                //get all content type configurations
                var contentTypeConfigurations = configuration.Descendants("ContentType");

                //create content types
                foreach (var contentTypeConfiguration in contentTypeConfigurations)
                {
                    var contentType = CreateContentType(contentTypeConfiguration);

                    _contentTypeService.Save(contentType);
                }

                //update content types
                foreach (var contentTypeConfiguration in contentTypeConfigurations)
                {
                    var contentType = UpdateContentType(contentTypeConfiguration);

                    _contentTypeService.Save(contentType);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error<ContentTypeCreator>("Failed to deply content types", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentTypeConfiguration"></param>
        /// <returns></returns>
        private ContentType CreateContentType(XElement contentTypeConfiguration)
        {
            try
            {
                //create content type
                var contentType = new ContentType(-1)
                {
                    Name = contentTypeConfiguration.Element("Name").Value,
                    Alias = contentTypeConfiguration.Element("Alias").Value,
                    AllowedAsRoot = bool.Parse(contentTypeConfiguration.Element("AllowedAsRoot").Value),
                    AllowedTemplates = GetTemplates(contentTypeConfiguration),
                    Thumbnail = contentTypeConfiguration.Element("Thumbnail").Value,
                    Description = contentTypeConfiguration.Element("Description").Value
                };

                //add properties
                foreach (var property in contentTypeConfiguration.Descendants("Property"))
                {
                    //add property to content type
                    contentType.AddPropertyType(
                        new PropertyType(
                            _dataTypeDefinitions.FirstOrDefault(t => t.Name.ToLower() == property.Element("Type").Value))
                        {
                            Name = property.Element("Name").Value,
                            Alias = property.Element("Alias").Value,
                            Description = property.Element("Description").Value,
                            Mandatory = bool.Parse(property.Element("Mandatory").Value)
                        },
                        property.Element("Group").Value);
                }

                return contentType;
            }
            catch (Exception ex)
            {
                LogHelper.Error<ContentTypeCreator>("Failed to create content type", ex);
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentTypeConfiguration"></param>
        /// <returns></returns>
        private IContentType UpdateContentType(XElement contentTypeConfiguration)
        {
            try
            {
                // get content type
                var contentType = _contentTypeService.GetContentType(contentTypeConfiguration.Element("Alias").Value.ToCleanString(CleanStringType.Alias | CleanStringType.UmbracoCase));

                //set default template
                contentType.SetDefaultTemplate(GetTemplate(contentTypeConfiguration.Element("DefaultTemplate").Value));

                //set allowed content types
                contentType.AllowedContentTypes = GetContentTypeSort(contentTypeConfiguration.Element("AllowedContentTypes").Value);

                //set composition
                contentType.ContentTypeComposition = GetContentTypes(contentTypeConfiguration.Element("ContentTypeComposition").Value);

                return contentType;
            }
            catch (Exception ex)
            {
                LogHelper.Error<ContentTypeCreator>("Failed to update content type", ex);
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private IEnumerable<ContentType> GetContentTypes(string aliases)
        {
            var aliasesArray = aliases.Split(',');

            return GetContentTypes(aliasesArray);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private IEnumerable<ContentType> GetContentTypes(IEnumerable<string> aliases)
        {
            var contentTypes = new List<ContentType>();

            foreach (var alias in aliases)
            {
                var contentType = _contentTypeService.GetContentType(alias);

                if (contentType != null)
                {
                    contentTypes.Add((ContentType)contentType);
                }
            }

            return contentTypes;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private IEnumerable<ContentTypeSort> GetContentTypeSort(string aliases)
        {
            var aliasesArray = aliases.Split(',');

            return GetContentTypeSort(aliasesArray);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private IEnumerable<ContentTypeSort> GetContentTypeSort(string[] aliases)
        {
            var contentTypeSort = new List<ContentTypeSort>();

            for (var i = 0; i < aliases.Length; i++)
            {
                var contentType = _contentTypeService.GetContentType(aliases[i]);

                if (contentType != null)
                {
                    contentTypeSort.Add(new ContentTypeSort(contentType.Id, i));
                }
            }

            return contentTypeSort;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private IEnumerable<ITemplate> GetTemplates(XElement e)
        {
            return e.Descendants("Template").Select(t => GetTemplate(t.Value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        private ITemplate GetTemplate(string alias)
        {
            return _fileService.GetTemplate(alias);
        }
    }
}