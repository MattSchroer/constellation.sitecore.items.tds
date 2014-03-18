namespace Constellation.Sitecore.Items.Tds
{
	using HedgehogDevelopment.SitecoreProject.VSIP.CodeGeneration.Models;
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;

	/// <summary>
	/// Methods that are useful for T4 Template code generation, and are meant to be called from the T4 Template.
	/// </summary>
	public class ParsingUtility
	{
		/// <summary>
		/// Given a SitecoreTemplate, returns a comma-delimited list of interfaces that represent
		/// the supplied Template's inheritance.
		/// </summary>
		/// <param name="namespace">
		/// The namespace of the Data Template, in C# format.
		/// </param>
		/// <param name="template">
		/// The Data Template to parse.
		/// </param>
		/// <returns>
		/// The inheritance chain for the strongly typed item.
		/// </returns>
		[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "Reviewed. Suppression is OK here.")]
		public static string GetInheritanceChain(string @namespace, SitecoreTemplate template)
		{
			var interfaces = GetInheritedInterfaces(new List<string>(), @namespace, template);

			return interfaces.Any() ? ", " + string.Join(", ", interfaces) : string.Empty;
		}

		/// <summary>
		/// Gets the fully qualified name of the supplied Sitecore Item.
		/// </summary>
		/// <param name="defaultNamespace">
		/// The default namespace.
		/// </param>
		/// <param name="item">
		/// The template item.
		/// </param>
		/// <returns>
		/// The namespace string of the Item.
		/// </returns>
		public static string GetFullyQualifiedName(string defaultNamespace, SitecoreItem item)
		{
			return GetFullyQualifiedName(defaultNamespace, item, s => s);
		}

		/// <summary>
		/// Gets the fully qualified name of the supplied Sitecore Item.
		/// </summary>
		/// <param name="defaultNamespace">
		/// The default namespace.
		/// </param>
		/// <param name="item">
		/// The template.
		/// </param>
		/// <param name="nameFunc">
		/// The function to run the template name through.
		/// </param>
		/// <returns>
		/// The <see cref="string"/>.
		/// </returns>
		public static string GetFullyQualifiedName(string defaultNamespace, SitecoreItem item, Func<string, string> nameFunc)
		{
			return string.Concat(GetNamespace(defaultNamespace, item, true), ".", nameFunc(item.Name));
		}

		/// <summary>
		/// Gets the calculated namespace for the template
		/// </summary>
		/// <param name="defaultNamespace">
		/// The default Namespace.
		/// </param>
		/// <param name="item">
		/// The item.
		/// </param>
		/// <param name="includeGlobal">
		/// The include Global.
		/// </param>
		/// <returns>
		/// The namespace of the supplied item.
		/// </returns>
		public static string GetNamespace(string defaultNamespace, SitecoreItem item, bool includeGlobal = false)
		{
			var namespaceSegments = new List<string>();

			////namespaceSegments.Add(defaultNamespace);
			namespaceSegments.Add(item.Namespace);
			var @namespace = namespaceSegments.AsNamespace().Replace(".sitecore.templates", string.Empty); // use an extension method in the supporting assembly

			return includeGlobal ? string.Concat("global::", @namespace) : @namespace;
		}

		/// <summary>
		/// Takes multiple namespaces ('.' delimited strings) and joins them together
		/// </summary>
		/// <param name="namespaces">
		/// The namespaces.
		/// </param>
		/// <returns>
		/// The concatenated namespaces.
		/// </returns>
		public static string JoinNamespaces(params string[] namespaces)
		{
			// leverage an extension method in the support assembly
			return namespaces.AsNamespace();
		}

		/// <summary>
		/// Gets a list of all fields from the template. 
		/// <para>
		/// If desired, fields from all base templates will be included.
		/// </para>
		/// </summary>
		/// <param name="item">
		/// The item.
		/// </param>
		/// <param name="includeBases">
		/// if set to <c>true</c> include base template's fields.
		/// </param>
		/// <returns>
		/// The list of fields.
		/// </returns>
		public static List<SitecoreField> GetFieldsForTemplate(SitecoreTemplate item, bool includeBases)
		{
			var fields = new List<SitecoreField>();

			// Add direct fields that aren't ignored
			fields.AddRange(item.Fields.Where(f => GetCustomProperty(f.Data, "ignore") != "true"));

			if (!includeBases)
			{
				return fields;
			}

			// leverage an extenstion method to recursively select base templates, then flatten the fields down
			var baseFields = item.BaseTemplates.SelectRecursive(i => i.BaseTemplates).SelectMany(t => t.Fields);

			// only grab base fields who aren't ignored
			fields.AddRange(baseFields.Where(f => GetCustomProperty(f.Data, "ignore") != "true"));

			return fields;
		}

		/// <summary>
		/// Given a sitecore field, returns the name of the property to use.
		/// <para>If the field is plural, it returns a plural property name</para>
		/// </summary>
		/// <param name="field">The field.</param>
		/// <returns>A string to use for a property representing the field</returns>
		public static string GetPropertyName(SitecoreField field)
		{
			// has the field been configured in TDS with a custom name?
			string customName = GetCustomProperty(field.Data, "name");
			if (!string.IsNullOrEmpty(customName))
			{
				return customName;
			}

			bool isFieldPlural = IsFieldPlural(field);

			return field.Name.AsPropertyName(isFieldPlural);
		}

		/// <summary>
		/// Determines whether the Sitecore Field holds multiple values.
		/// </summary>
		/// <param name="field">The field.</param>
		/// <returns>
		///   <c>true</c> if the field holds multiple values; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsFieldPlural(SitecoreField field)
		{
			var multipleValueFields = new[]
				{
					"treelist",
					"treelistex",
					"treelist descriptive",
					"checklist",
					"multilist"
				};

			return multipleValueFields.Contains(field.Type.ToLower());
		}

		/// <summary>
		/// Gets a custom property from the data assuming it is query string format
		/// </summary>
		/// <param name="data">A string in query string format.</param>
		/// <param name="key">The key to get the value for.</param>
		/// <returns>The value, or an empty string</returns>
		public static string GetCustomProperty(string data, string key)
		{
			if (string.IsNullOrEmpty(data))
			{
				return string.Empty;
			}

			var strArray = data.Split(new[] { '&' });
			var keyEquals = key + "=";
			var length = keyEquals.Length;

			foreach (var keyValuePair in strArray)
			{
				if ((keyValuePair.Length > length) && keyValuePair.StartsWith(keyEquals, StringComparison.OrdinalIgnoreCase))
				{
					return keyValuePair.Substring(length);
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Given a Template, walks up the Inherited Templates fields of the Template's ancestors
		/// to create a complete list of inherited Templates.
		/// </summary>
		/// <param name="interfaces">
		/// The list of interfaces to append.
		/// </param>
		/// <param name="_namespace">
		/// The Namespace of the Template being parsed.
		/// </param>
		/// <param name="template">
		/// The Data Template to parse.
		/// </param>
		/// <returns>
		/// A complete list of inherited template names.
		/// </returns>
		[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1309:FieldNamesMustNotBeginWithUnderscore", Justification = "Reviewed. Suppression is OK here.")]
		private static List<string> GetInheritedInterfaces(List<string> interfaces, string _namespace, SitecoreTemplate template)
		{
			if (interfaces == null)
			{
				interfaces = new List<string>();
			}

			foreach (var baseTemplate in template.BaseTemplates)
			{
				interfaces.Add(
					string.Concat(
						GetFullyQualifiedName(_namespace, baseTemplate)
							.Substring(0, GetFullyQualifiedName(_namespace, baseTemplate).LastIndexOf(".", StringComparison.OrdinalIgnoreCase)),
						".",
						baseTemplate.Name.AsInterfaceName()));

				interfaces = GetInheritedInterfaces(interfaces, _namespace, baseTemplate);
			}

			return interfaces;
		}
	}
}
