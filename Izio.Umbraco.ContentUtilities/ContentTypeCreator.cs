using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

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
            var contentTypeConfigurations = configuration.Descendants("ContentType").Select(e => CreateContentType(e));

            foreach (var contentType in contentTypeConfigurations)
            {
                _contentTypeService.Save(contentType);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private ContentType CreateContentType(XElement e)
        {
            try
            {
                //create content type
                var contentType = new ContentType(-1)
                {
                    Name = e.Element("Name").Value,
                    Alias = e.Element("Alias").Value,
                    AllowedAsRoot = bool.Parse(e.Element("AllowedAsRoot").Value),
                    AllowedTemplates = GetTemplates(e),
                    Thumbnail = e.Element("Thumbnail").Value,
                    Description = e.Element("Description").Value
                };

                //add properties
                foreach (var property in e.Descendants("Property"))
                {
                    //add property to content type
                    contentType.AddPropertyType(
                        new PropertyType(_dataTypeDefinitions.FirstOrDefault(t => t.Name.ToLower() == property.Element("Type").Value))
                        {
                            Name = property.Element("Name").Value,
                            Alias = property.Element("Alias").Value,
                            Description = property.Element("Description").Value,
                            Mandatory = bool.Parse(property.Element("Mandatory").Value)
                        },
                        property.Element("Group").Value);
                }

                //set default template
                contentType.SetDefaultTemplate(GetTemplate(e.Element("DefaultTemplate").Value));

                //set allowed content types
                contentType.AllowedContentTypes = GetContentTypes(e.Element("AllowedContentTypes").Value);

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
        /// <param name="v"></param>
        /// <returns></returns>
        private IEnumerable<ContentTypeSort> GetContentTypes(string v)
        {
            var aliases = v.Split(',');
            var contentTypeSort = new List<ContentTypeSort>();


            for (int i = 0; i < aliases.Length; i++)
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