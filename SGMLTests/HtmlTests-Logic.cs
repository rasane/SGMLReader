﻿/*
 * 
 * Copyright (c) 2007-2011 MindTouch. All rights reserved.
 * 
 */

using System;
using System.IO;
using System.Xml;
using log4net;
using NUnit.Framework;
using Sgml;

namespace SGMLTests {
    public partial class UnitTests {

        //--- Types ---
        private delegate void SgmlReaderTestCallback(SgmlReader reader, XmlWriter xmlWriter);
        private enum XmlRender {
            Doc,
            DocClone,
            Passthrough
        }

        //--- Class Fields ---
        private static ILog _log = LogManager.GetLogger(typeof(UnitTests));

        //--- Class Methods ---
        private static void Test(string name, XmlRender xmlRender, CaseFolding caseFolding, string doctype, bool format) {
            string source;
            string expected;
            ReadTest(name, out source, out expected);
            expected = expected.Trim().Replace("\r", "");
            string actual;

            // determine how the document should be written back
            SgmlReaderTestCallback callback;
            switch(xmlRender) {
            case XmlRender.Doc:

                // test writing sgml reader using xml document load
                callback = (reader, writer) => {
                    var doc = new XmlDocument();
                    doc.Load(reader);
                    doc.WriteTo(writer);
                };
                break;
            case XmlRender.DocClone:

                // test writing sgml reader using xml document load and clone
                callback = (reader, writer) => {
                    var doc = new XmlDocument();
                    doc.Load(reader);
                    var clone = doc.Clone();
                    clone.WriteTo(writer);
                };
                break;
            case XmlRender.Passthrough:

                // test writing sgml reader directly to xml writer
                callback = (reader, writer) => {
                    reader.Read();
                    while(!reader.EOF) {
                        writer.WriteNode(reader, true);
                    }
                };
                break;
            default:
                throw new ArgumentException("unknown value", "xmlRender");
            }
            actual = RunTest(caseFolding, doctype, format, source, callback);
            Assert.AreEqual(expected, actual);
        }

        private static void ReadTest(string name, out string before, out string after) {
            var assembly = typeof(UnitTests).Assembly;
            var stream = assembly.GetManifestResourceStream(assembly.FullName.Split(',')[0] + ".Resources." + name);
            if(stream == null) {
                throw new FileNotFoundException("unable to load requested resource: " + name);
            }
            using(var sr = new StreamReader(stream)) {
                var test = sr.ReadToEnd().Split('`');
                before = test[0];
                after = test[1];
            }
        }

        private static string RunTest(CaseFolding caseFolding, string doctype, bool format, string source, SgmlReaderTestCallback callback) {

            // initialize sgml reader
            var reader = new SgmlReader {
                CaseFolding = caseFolding,
                DocType = doctype,
                InputStream = new StringReader(source),
                WhitespaceHandling = format ? WhitespaceHandling.None : WhitespaceHandling.All
            };

            // initialize xml writer
            var stringWriter = new StringWriter();
            var xmlTextWriter = new XmlTextWriter(stringWriter);
            if(format) {
                xmlTextWriter.Formatting = Formatting.Indented;
            }
            callback(reader, xmlTextWriter);
            xmlTextWriter.Close();

            // reproduce the parsed document
            var actual = stringWriter.ToString();

            // ensure that output can be parsed again
            try {
                using(var stringReader = new StringReader(actual)) {
                    var doc = new XmlDocument();
                    doc.Load(stringReader);
                }
            } catch(Exception e) {
                Assert.Fail("unable to parse sgml reader output:\n{0}", actual);
            }
            return actual.Trim().Replace("\r", "");
        }
    }
}
