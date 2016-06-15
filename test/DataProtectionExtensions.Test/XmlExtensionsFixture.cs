using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using DataProtectionExtensions;
using Xunit;

namespace DataProtectionExtensions.Test
{
	public class XmlExtensionsFixture
	{
		[Fact]
		public void ElementToProcess_GetsCorrectElement()
		{
			var doc = new XmlDocument();
			doc.LoadXml("<root><test node=\"1\">text</test></root>");
			var element = doc.ElementToProcess();
			Assert.Equal("<test node=\"1\">text</test>", element.OuterXml);
		}

		[Fact]
		public void ElementToProcess_NullDocument()
		{
			Assert.Throws<ArgumentNullException>(() => XmlExtensions.ElementToProcess(null));
		}

		[Fact]
		public void ToXElement_ConvertsNode()
		{
			string xml = "<root><test node=\"1\">text</test></root>";
			var doc = new XmlDocument();
			doc.LoadXml(xml);
			var el = doc.ToXElement();
			Assert.Equal(xml, el.ToString(SaveOptions.DisableFormatting));
		}

		[Fact]
		public void ToXElement_NullNode()
		{
			Assert.Throws<ArgumentNullException>(() => XmlExtensions.ToXElement(null));
		}

		[Fact]
		public void ToXmlDocumentWithRootNode_ElementConverted()
		{
			var element = XElement.Parse("<test node=\"1\">text</test>");
			var doc = element.ToXmlDocumentWithRootNode();
			Assert.Equal("<root><test node=\"1\">text</test></root>", doc.OuterXml);
		}

		[Fact]
		public void ToXmlDocumentWithRootNode_NullElement()
		{
			Assert.Throws<ArgumentNullException>(() => XmlExtensions.ToXmlDocumentWithRootNode(null));
		}

		[Fact]
		public void XmlExtensions_Integration()
		{
			// These extensions work together during encryption/decryption
			// operations so this test validates their interaction.
			var expected = "<test node=\"1\">text</test>";
			var actual = XElement.Parse(expected)
				.ToXmlDocumentWithRootNode()
				.ElementToProcess()
				.ToXElement()
				.ToString(SaveOptions.DisableFormatting);

			Assert.Equal(expected, actual);
		}
	}
}
