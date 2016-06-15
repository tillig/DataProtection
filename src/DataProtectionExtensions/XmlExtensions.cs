using System;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Xml;
using System.Xml.Linq;

namespace DataProtectionExtensions
{
	/// <summary>
	/// Extension methods used for working with XML in the context
	/// of data protection operations.
	/// </summary>
	/// <remarks>
	/// In working with <see cref="EncryptedXml"/>, we need to convert nodes back and forth
	/// between <see cref="XElement"/> and <see cref="XmlDocument"/>. These extensions help in that conversion.
	/// </remarks>
	public static class XmlExtensions
	{
		/// <summary>
		/// Gets the <see cref="XmlElement"/> that should be processed
		/// by <see cref="EncryptedXml"/> after conversion from <see cref="XElement"/>
		/// to <see cref="XmlDocument"/>.
		/// </summary>
		/// <param name="document">
		/// The document converted by <see cref="ToXmlDocumentWithRootNode(XElement)"/>.
		/// </param>
		/// <returns>
		/// The <see cref="XmlElement"/> corresponding to the <see cref="XElement"/>
		/// that was originally converted.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="document" /> is <see langword="null" />.
		/// </exception>
		public static XmlElement ElementToProcess(this XmlDocument document)
		{
			if (document == null)
			{
				throw new ArgumentNullException(nameof(document));
			}

			return (XmlElement)document.DocumentElement.FirstChild;
		}

		/// <summary>
		/// Takes an <see cref="XmlNode"/> and converts it to an <see cref="XElement"/>.
		/// </summary>
		/// <param name="node">
		/// The node to convert.
		/// </param>
		/// <returns>
		/// An <see cref="XElement"/> with the <paramref name="node" />
		/// XML contents.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="node" /> is <see langword="null" />.
		/// </exception>
		public static XElement ToXElement(this XmlNode node)
		{
			if (node == null)
			{
				throw new ArgumentNullException(nameof(node));
			}

			return XElement.Load(node.CreateNavigator().ReadSubtree());
		}

		/// <summary>
		/// Converts an <see cref="XElement"/> to an <see cref="XmlDocument"/>
		/// that can be processed by <see cref="EncryptedXml"/>.
		/// </summary>
		/// <param name="element">
		/// The <see cref="XElement"/> to encrypt using <see cref="EncryptedXml"/>.
		/// </param>
		/// <returns>
		/// An <see cref="XmlDocument"/> that can be used along with
		/// <see cref="ElementToProcess(XmlDocument)"/> to encrypt the
		/// <paramref name="element" /> with <see cref="EncryptedXml"/>.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">
		/// Thrown if <paramref name="element" /> is <see langword="null" />.
		/// </exception>
		public static XmlDocument ToXmlDocumentWithRootNode(this XElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			// EncryptedXml needs an XmlDocument so we create a dummy doc with a
			// <root /> element.
			var xmlDocument = new XmlDocument();
			xmlDocument.Load(new XElement("root", element).CreateReader());
			return xmlDocument;
		}
	}
}
