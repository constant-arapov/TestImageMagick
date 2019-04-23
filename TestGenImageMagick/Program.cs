using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace TestGenImageMagick
{
    class Conversion
    {
        public string Source { get; set; }
        public string Destination { get; set; }

        public Conversion(string source, string destination)
        {
            Source = source;
            Destination = destination;
        }
    }

    public class FormatAttributes
    {
        public string Name { get; set; }
        public string Mode { get; set; }
    }

    class Program
    {

        private static List<string> _lstNotGenerateTests = new List<string>()
        {
            "jpt",
            "mrw",
            "msl",
            "pix",
            "pwp",
            "rle",
            "sfw",
            "tim",
            "clipboard"
        };

        static void Main(string[] args)
        {
            RetrieveDataFromAllTestsResults();

            List<FormatAttributes> lstformatAttributes = new List<FormatAttributes>();

            var res =  File.ReadAllLines("format_attributes.txt").ToList<string>();
            res.ForEach(o =>
            {
               var ln =  o.Split('\t');
               lstformatAttributes.Add(new FormatAttributes{Name = ln[0].ToLower(), Mode = ln[1]});
            });

            StringBuilder builderBmp = new StringBuilder();

            //generate all writeable format file from bmp
            foreach (var el in lstformatAttributes)
            {
                if (el.Mode.Contains('W'))
                    builderBmp.Append($"\t\t[TestCase(\"test.bmp\", \"test.{el.Name}\")]\n");
            }

            var bmpBuild =  builderBmp.ToString();

            var builderProj = new StringBuilder();

            foreach (var el in lstformatAttributes)
            {
                if (el.Mode.Contains('R'))
                {
                    builderProj.Append($"   <Content Include=\"Input\\test.{el.Name}\">\n");
                    builderProj.Append("     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\n");
                    builderProj.Append("   </Content>\n");
                }
            }

            var projAdd = builderProj.ToString();

            
            List<string> formats = new List<string>()
            {
                "bmp",
                "jpg",
                "jpeg",
                "png",
                "gif",
                "tiff",
                "tif",
                "emf",
                "wmf",
                "psd"
            };

            //to emf not supported ?
            //to wmf not supported ?

            List<Conversion> lstNotSupportedConvs = new List<Conversion>()
            {
               /* new NotSupportedConv("bmp", "emf"),
                new NotSupportedConv("bmp", "wmf"),
                new NotSupportedConv("emf", "wmf"),
                new NotSupportedConv("gif", "emf"),
                new NotSupportedConv("gif", "emf"),
                new NotSupportedConv("gif", "wmf"),
                new NotSupportedConv("jpeg", "emf"),
                new NotSupportedConv("jpeg", "wmf"),
                new NotSupportedConv("jpg", "emf"),
                new NotSupportedConv("jpg", "wmf"),
                new NotSupportedConv("png", "emf"),
                new NotSupportedConv("png", "wmf"),
                new NotSupportedConv("psd", "emf"),
                new NotSupportedConv("psd", "wmf"),
                new NotSupportedConv("tif", "emf"),
                new NotSupportedConv("tif", "wmf"),
                new NotSupportedConv("tiff", "emf"),
                new NotSupportedConv("tiff", "wmf"),
                new NotSupportedConv("wmf", "emf"),*/
            };


           

            StringBuilder builder = new StringBuilder();
           
            foreach (var extFormt in formats)
            {
                var extFromtUse = char.ToUpper(extFormt[0]) + extFormt.Substring(1, extFormt.Length - 1);
                
                string convToId = $"ConvertTo{extFromtUse}";
                builder.Append($"  <AssemblySkill Id=\"{convToId}\">\n");
                builder.Append( "    <Accepts>\n");
                foreach (var intFormt in formats)
                {
                    bool bNotSupported =
                        (lstNotSupportedConvs.Find(el => el.Source == intFormt  && el.Destination == extFormt) != null);

                                                                               
                    if (extFormt != intFormt && !bNotSupported)
                    {
                        builder.Append($"      <FileExtension>{intFormt}</FileExtension>\n");
                    }
                }
                builder.Append("    </Accepts>\n");
               
                builder.Append("    <Commands Weight=\"500\">\n");
                builder.Append($"     <ConvertPattern Extensions=\"{extFormt}\"/>\n");
                builder.Append("    </Commands>\n");
                builder.Append("    <Parameters>\n");
                builder.Append("    </Parameters>\n");
                builder.Append("    <Action Type=\"Filestar.Plugin.ImageMagick.ConvertSkill\"/>\n");
                builder.Append($"  </AssemblySkill>\n\n");
            }

            var resManifest = builder.ToString();
            builder.Clear();

            foreach (var extFormt in lstformatAttributes)
            {
                if (!extFormt.Mode.Contains("W"))
                    continue;
                
                builder.Append($"\t//Start tests convert to {extFormt.Name}\n");
                foreach (var intFormt in lstformatAttributes)
                {
                    if (intFormt.Name.Contains("brf"))
                        System.Threading.Thread.Sleep(0);


                    bool bNotSupported = !intFormt.Mode.Contains("R") || _lstNotGenerateTests.Contains(intFormt.Name);
                        //(lstNotSupportedConvs.Find(el => el.Source == intFormt && el.Destination == extFormt) != null);

                    if (extFormt.Name != intFormt.Name && !bNotSupported)
                    {
                        builder.Append($"\t\t[TestCase(\"test.{intFormt.Name}\", \"test_{intFormt.Name}.{extFormt.Name}\")]\n");
                    }
                }
                builder.Append($"\t//End tests convert to {extFormt.Name}\n\n");
            }

            var resTestFixtures = builder.ToString();

            builder.Clear();

            foreach (var extFormt in lstformatAttributes)
            {
                if (!_lstNotGenerateTests.Contains(extFormt.Name) && extFormt.Mode.Contains("R"))
                {
                    builder.Append($"   <Content Include=\"Input\\test.{extFormt.Name}\">\n");
                    builder.Append("     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>\n");
                    builder.Append("   </Content>\n");
                }
            }

            var resCsProj = builder.ToString();

        }

        private static void RetrieveDataFromAllTestsResults()
        {
            List<string> lines = File.ReadAllLines(@"..\..\..\..\result_2019_04_23_01.txt")
                .ToList<string>();

            List<Conversion> _lsAllowedConversions = new List<Conversion>();

            foreach (var ln in lines)
            {
                //string expr = "TestConvertFrom(\"test.bmp\", \"test_bmp.aai\") Success";
                // var regex = new Regex("TestConvertFrom.* Success");
                var regex = new Regex("TestConvertFrom.*\"(.*)..*\"*Success");
                //var res = regex.IsMatch(ln);
                //if (res)
                // System.Threading.Thread.Sleep(0);

                var groups =((new Regex("TestConvertFrom.*\"test[.](.*)\",\".*[.](.*)\".*Success")).Match(ln)).Groups;
                if (groups.Count > 1)
                {
                    string source = groups[1].ToString();
                    string destination = groups[2].ToString();
                   
                    _lsAllowedConversions.Add(new Conversion(source,destination));
                }

            }

            //var el = _lsAllowedConversions.GroupBy(o => o.Destination).Select(new string() as a);
          // var res = from p in _lsAllowedConversions
              //  group p.Destination by p.Source into g
              //  select new { Destination = g.Key, Source = g.ToList() };
              var allowedConversions = _lsAllowedConversions.GroupBy(
                  d => d.Destination,
                  s => s.Source,
                  (key, g) => new {DestRes = key, SourceRes = g.ToList()}).OrderBy(o=>o.DestRes);

              var builder  = new StringBuilder();
            
              foreach (var groupConv in allowedConversions)
              {
                  //special filter for "hdr" dublicate remove no need in normal case
                  int ind = groupConv.SourceRes.FindLastIndex (o => o == "hdr");
                  if (ind>0)
                    groupConv.SourceRes.RemoveAt(ind);
                builder.Append($"\t//Start tests convert to {groupConv.DestRes}\n");

                foreach (var convSource in groupConv.SourceRes)
                  {
                      if (groupConv.DestRes != convSource)
                      {
                          builder.Append(
                              $"\t\t[TestCase(\"test.{convSource}\", \"test_{convSource}.{groupConv.DestRes}\")]\n");
                      }
                  }

                builder.Append($"\t//End tests convert to {groupConv.DestRes}\n\n");
            }

              builder.Clear();
              var resTests = builder.ToString();

              foreach (var groupConv in allowedConversions)
              {
                  var extFromtUse = char.ToUpper(groupConv.DestRes[0]) + groupConv.DestRes.Substring(1, groupConv.DestRes.Length - 1);

                  string convToId = $"ConvertTo{extFromtUse}";
                  builder.Append($"  <AssemblySkill Id=\"{convToId}\">\n");
                  builder.Append("    <Accepts>\n");
                  foreach (var intFormt in groupConv.SourceRes)
                  {                    
                      if (groupConv.DestRes != intFormt)
                      {
                          builder.Append($"      <FileExtension>{intFormt}</FileExtension>\n");
                      }
                  }
                  builder.Append("    </Accepts>\n");

                  builder.Append("    <Commands Weight=\"500\">\n");
                  builder.Append($"     <ConvertPattern Extensions=\"{groupConv.DestRes}\"/>\n");
                  builder.Append("    </Commands>\n");
                  builder.Append("    <Parameters>\n");
                  builder.Append("    </Parameters>\n");
                  builder.Append("    <Action Type=\"Filestar.Plugin.ImageMagick.ConvertSkill\"/>\n");
                  builder.Append($"  </AssemblySkill>\n\n");
              }

              var manifest = builder.ToString();
        }



     
    }
}
