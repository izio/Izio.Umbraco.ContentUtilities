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
        private readonly List<ContentType> _deployedContentTypes;

        /// <summary>
        /// 
        /// </summary>
        public ContentTypeCreator()
        {
            _contentTypeService = ApplicationContext.Current.Services.ContentTypeService;
            _fileService = ApplicationContext.Current.Services.FileService;
            _dataTypeDefinitions = ApplicationContext.Current.Services.DataTypeService.GetAllDataTypeDefinitions();
            _deployedContentTypes = new List<ContentType>();
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

                //get all content type configurations
                var contentTypeConfigurations = configuration.Descendants("ContentType");

                //create content types
                foreach (var contentTypeConfiguration in contentTypeConfigurations)
                {
                    var contentType = CreateContentType(contentTypeConfiguration);

                    _deployedContentTypes.Add(contentType);
                }

                //update content types
                foreach (var contentTypeConfiguration in contentTypeConfigurations)
                {
                    UpdateContentType(contentTypeConfiguration);
                }
            }
            catch (Exception ex)
            {
                //log exception
                LogHelper.Error<ContentTypeCreator>("Failed to deply content types", ex);

                //delete deployed content types
                foreach (var contentType in _deployedContentTypes)
                {
                    _contentTypeService.Delete(contentType);
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
            //get existing aliases
            var existingAliases = _contentTypeService.GetAllContentTypeAliases();

            //check whether there are any clashes with existing content type aliases
            if (aliases.Intersect(existingAliases).Any())
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentTypeConfiguration"></param>
        /// <returns></returns>
        private ContentType CreateContentType(XElement contentTypeConfiguration)
        {
            //create content type
            var contentType = new ContentType(-1)
            {
                Name = contentTypeConfiguration.Element("Name").Value,
                Alias = contentTypeConfiguration.Element("Alias").Value,
                AllowedAsRoot = bool.Parse(contentTypeConfiguration.Element("AllowedAsRoot").Value),
                AllowedTemplates = GetTemplates(contentTypeConfiguration.Element("AllowedTemplates").Value),
                Thumbnail = contentTypeConfiguration.Element("Thumbnail").Value,
                Description = contentTypeConfiguration.Element("Description").Value
            };

            //add properties
            foreach (var property in contentTypeConfiguration.Descendants("Property"))
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

            //save content type
            _contentTypeService.Save(contentType);

            return contentType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentTypeConfiguration"></param>
        /// <returns></returns>
        private IContentType UpdateContentType(XElement contentTypeConfiguration)
        {
            // get content type
            var contentType = _contentTypeService.GetContentType(contentTypeConfiguration.Element("Alias").Value.ToCleanString(CleanStringType.Alias | CleanStringType.UmbracoCase));

            return UpdateContentType(contentType, contentTypeConfiguration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentTypeConfiguration"></param>
        /// <returns></returns>
        private IContentType UpdateContentType(IContentType contentType, XElement contentTypeConfiguration)
        {
            //set default template
            contentType.SetDefaultTemplate(GetTemplate(contentTypeConfiguration.Element("DefaultTemplate").Value));

            //set allowed content types
            contentType.AllowedContentTypes = GetContentTypeSort(contentTypeConfiguration.Element("AllowedContentTypes").Value);

            //set composition
            contentType.ContentTypeComposition = GetContentTypes(contentTypeConfiguration.Element("ContentTypeComposition").Value);

            //save the changes
            _contentTypeService.Save(contentType);

            return contentType;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private IEnumerable<ContentType> GetContentTypes(string aliases)
        {
            //split aliases in to an array
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
            //create content type list
            var contentTypes = new List<ContentType>();

            foreach (var alias in aliases)
            {
                //get content type by alias
                var contentType = _contentTypeService.GetContentType(alias);

                //if content type exists add to list
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
            //split aliases in to an array
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
            //create content type sort list
            var contentTypeSort = new List<ContentTypeSort>();

            for (var i = 0; i < aliases.Length; i++)
            {
                //get content type
                var contentType = _contentTypeService.GetContentType(aliases[i]);

                //if content type exists add to list in current order
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
        /// <param name="aliases"></param>
        /// <returns></returns>
        private IEnumerable<ITemplate> GetTemplates(string aliases)
        {
            //split aliases in to an array
            var templatesArray = aliases.Split(',');

            return GetTemplates(templatesArray);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="aliases"></param>
        /// <returns></returns>
        private IEnumerable<ITemplate> GetTemplates(string[] aliases)
        {
            //create ITemplate list
            var templates = new List<ITemplate>();

            foreach (var alias in aliases)
            {
                //get template
                var template = GetTemplate(alias.ToCleanString(CleanStringType.UnderscoreAlias));

                //if template exists add to list
                if (template != null)
                {
                    templates.Add(template);
                }
            }

            return templates;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="alias"></param>
        /// <returns></returns>
        private ITemplate GetTemplate(string alias)
        {
            //get specified template by alias
            return _fileService.GetTemplate(alias.ToCleanString(CleanStringType.UnderscoreAlias));
        }
    }
}