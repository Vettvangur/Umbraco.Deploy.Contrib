using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Umbraco.Core;
using Umbraco.Core.Deploy;
using Umbraco.Core.Logging;
using Umbraco.Core.Models;
using Umbraco.Core.Services;

namespace Umbraco.Deploy.ValueConnectors
{
	/// <summary>
	/// Implements a value connector for the link picker (Gibe.LinkPicker)
	/// https://our.umbraco.org/projects/backoffice-extensions/link-picker/
	/// </summary>
	public class LinkPickerValueConnector : IValueConnector
    {
        private readonly IEntityService _entityService;
		private readonly ILogger _logger;

		/// <summary>
		/// Initializes a new instance of the <see cref="LinkPickerValueConnector"/> class.
		/// </summary>
		/// <param name="entityService"></param>
		public LinkPickerValueConnector(IEntityService entityService, ILogger logger)
        {
			_entityService = entityService ?? throw new ArgumentNullException(nameof(entityService));
			_logger = logger;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<string> PropertyEditorAliases => new[] { "Gibe.LinkPicker" };

        /// <inheritdoc/>
        public string GetValue(Property property, ICollection<ArtifactDependency> dependencies)
        {
			var svalue = property?.Value as string;

			if (string.IsNullOrWhiteSpace(svalue))
                return string.Empty;

            var linkPickerData = JsonConvert.DeserializeObject<LinkPickerModel>(svalue);

			// If the contentId/mediaId of the TypeData is set try get the GuidUdi for the content/media and
			// mark it as a dependency we need to deploy.
			// We need the Guid for the content/media because the integer value could be different in the different environments.
			if (TryGetGuidUdi(linkPickerData.Id, UmbracoObjectTypes.Document, Constants.UdiEntityType.Document, out GuidUdi contentGuidUdi))
			{

				dependencies.Add(new ArtifactDependency(contentGuidUdi, false, ArtifactDependencyMode.Exist));
				linkPickerData.Id = contentGuidUdi.Guid;
			}

			return JsonConvert.SerializeObject(linkPickerData);
        }

        /// <inheritdoc/>
        public void SetValue(IContentBase content, string alias, string value)
        {
			if (string.IsNullOrWhiteSpace(value))
            {
                content.SetValue(alias, value);
                return;
            }

            var linkPickerData = JsonConvert.DeserializeObject<LinkPickerModel>(value);

			// When we set the value we want to switch the Guid value of the contentId/mediaId to the integervalue
			// as this is what the UrlPicker uses to lookup it's content/media
			if (TryGetId(linkPickerData.Id, UmbracoObjectTypes.Document, out int contentId))
			{
				linkPickerData.Id = contentId;
			}

			content.SetValue(alias, JsonConvert.SerializeObject(linkPickerData));
        }

        private bool TryGetGuidUdi(object value, UmbracoObjectTypes umbracoObjectType, string entityType, out GuidUdi udi)
        {
			if (value != null && int.TryParse(value.ToString(), out int id))
			{
				var guidAttempt = _entityService.GetKeyForId(id, umbracoObjectType);
				if (guidAttempt.Success)
				{
					udi = new GuidUdi(entityType, guidAttempt.Result);
					return true;
				}
			}
			udi = null;
            return false;
        }

        private bool TryGetId(object value, UmbracoObjectTypes umbracoObjectType, out int id)
        {
			if (value != null && Guid.TryParse(value.ToString(), out Guid key))
			{
				var intAttempt = _entityService.GetIdForKey(key, umbracoObjectType);
				if (intAttempt.Success)
				{
					id = intAttempt.Result;
					return true;
				}
			}
			id = 0;
            return false;
        }

		internal class LinkPickerModel
		{
			[JsonProperty("id")]
			public object Id { get; set; }
			[JsonProperty("name")]
			public string Name { get; set; }
			[JsonProperty("url")]
			public string Url { get; set; }
			[JsonProperty("target")]
			public string Target { get; set; }
			[JsonProperty("hashtarget")]
			public string Hashtarget { get; set; }
		}
    }
}
