using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace LaboratoMDM.PolicyEngine.Tests.ADMX
{
    public class UniqueAdmxTagCollectorTest
    {
        [Fact]
        public void CollectAllUniqueXmlTagsAttributesAndChildTags()
        {
            var (tags, tagAttributes, childTags) = CollectTagsAttributesAndChildTags(@"C:\PolicyDefinitions", "*.admx");
            Assert.NotEmpty(tags);

            var policyTagHierarhy = childTags.FirstOrDefault(x => x.Key == "policy").Value;
            var childTagsOfPolicyChilds = new Dictionary<string, HashSet<string>>();
            var childTagsOfPolicyAttributes = new Dictionary<string, HashSet<string>>();
            foreach (var tag in policyTagHierarhy)
            {
                var hierarhyOfChild = childTags.FirstOrDefault(x => x.Key == tag).Value;
                childTagsOfPolicyChilds.Add(tag, hierarhyOfChild);

                var attributes = tagAttributes.FirstOrDefault(x => x.Key == tag).Value;
                childTagsOfPolicyAttributes.Add(tag, attributes);
            }

            var childTagsJson = JsonConvert.SerializeObject(childTags);
            var tagAttributesJson = JsonConvert.SerializeObject(tagAttributes);
            Console.WriteLine("End");
        }

        private (List<string> tags, Dictionary<string, HashSet<string>> attributes, Dictionary<string, HashSet<string>> childTags)
            CollectTagsAttributesAndChildTags(string folderPath, string extension)
        {
            var xmlFiles = Directory.GetFiles(folderPath, extension, SearchOption.AllDirectories);

            var uniqueTags = new HashSet<string>();
            var tagAttributes = new Dictionary<string, HashSet<string>>();
            var tagChildTags = new Dictionary<string, HashSet<string>>();

            foreach (var file in xmlFiles)
            {
                XDocument doc;
                try
                {
                    doc = XDocument.Load(file);
                }
                catch
                {
                    continue;
                }

                CollectRecursive(doc.Root!, uniqueTags, tagAttributes, tagChildTags);
            }

            return (uniqueTags.OrderBy(t => t).ToList(), tagAttributes, tagChildTags);
        }

        private void CollectRecursive(
            XElement element,
            HashSet<string> tags,
            Dictionary<string, HashSet<string>> tagAttributes,
            Dictionary<string, HashSet<string>> tagChildTags)
        {
            if (element == null) return;

            var tagName = element.Name.LocalName;
            tags.Add(tagName);

            if (!tagAttributes.ContainsKey(tagName))
                tagAttributes[tagName] = new HashSet<string>();

            foreach (var attr in element.Attributes())
                tagAttributes[tagName].Add(attr.Name.LocalName);

            if (!tagChildTags.ContainsKey(tagName))
                tagChildTags[tagName] = new HashSet<string>();

            foreach (var child in element.Elements())
            {
                tagChildTags[tagName].Add(child.Name.LocalName);
                CollectRecursive(child, tags, tagAttributes, tagChildTags);
            }
        }
    }
}
