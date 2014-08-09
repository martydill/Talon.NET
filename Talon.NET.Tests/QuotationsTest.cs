using System.IO;
using System.Linq;
using MimeKit;
using NUnit.Framework;
using Shouldly;

namespace Talon.Tests
{
    public class QuotationsTest
    {
        [Test]
        public void test_mark_message_lines_1()
        {
            // e - empty line
            // s - splitter line
            // m - line starting with quotation marker ">"
            // t - the rest

            var lines = new[]
            {
                "Hello", "",
                //# next line should be marked as splitter
                "_____________",
                "From: foo@bar.com",
                "",
                "> Hi",
                "",
                "Signature"
            };
            Quotations.MarkMessageLines(lines).ShouldBe("tessemet");
        }

        [Test]
        public void test_mark_message_lines_2()
        {
            var lines = new[]
            {
                "Just testing the email reply",
                "",
                "Robert J Samson",
                "Sent from my iPhone",
                "",
                //# all 3 next lines should be marked as splitters
                "On Nov 30, 2011, at 12:47 PM, Skapture <",
                "416ffd3258d4d2fa4c85cfa4c44e1721d66e3e8f4@skapture-staging.mailgun.org>",
                "wrote:",
                "",
                "Tarmo Lehtpuu has posted the following message on"
            };
            Quotations.MarkMessageLines(lines).ShouldBe("tettessset");
        }

        [Test]
        public void test_process_marked_lines_1()
        {
            // quotations and last message lines are mixed
            // consider all to be a last message
            var markers = "tsemmtetm".ToCharArray();
            var lines = Enumerable.Range(1, markers.Length).Select(i => i.ToString()).ToArray();
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(lines);
        }

        [Test]
        public void test_process_marked_lines_2()
        {
            // no splitter => no markers
            var markers = "tmm".ToCharArray();
            var lines = new[] { "1", "2", "3" };
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(lines);
        }

        [Test]
        public void test_process_marked_lines_3()
        {
            // text after splitter without markers is quotation
            var markers = "tst".ToCharArray();
            var lines = new[] { "1", "2", "3" };
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(new[] { "1" });
        }

        [Test]
        public void test_process_marked_lines_4()
        {
            // message + quotation + signature
            var markers = "tsmt".ToCharArray();
            var lines = new[] { "1", "2", "3", "4" };
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(new[] { "1", "4" });
        }

        [Test]
        public void test_process_marked_lines_5()
        {
            // message + <quotation without markers> + nested quotation
            var markers = "tstsmt".ToCharArray();
            var lines = new[] { "1", "2", "3", "4", "5", "6" };
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(new[] { "1" });
        }

        [Test]
        public void test_process_marked_lines_6()
        {
            // test links wrapped with paranthesis
            // link starts on the marker line
            var markers = "tsmttem".ToCharArray();
            var lines = new[]{"text",
                      "splitter",
                      ">View (http://example.com",
                      "/abc",
                      ")",
                      "",
                      "> quote"};
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(lines.Take(1));
        }

        [Test]
        public void test_process_marked_lines_7()
        {
            // link starts on the new line
            var markers = "tmmmtm".ToCharArray();
            var lines = new[]{"text",
                      ">",
                      ">",
                      ">",
                      "(http://example.com) >  ",
                      "> life is short. (http://example.com)  "
                     };
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(lines.Take(1));
        }

        [Test]
        public void test_process_marked_lines_8()
        {
            // check all "inline" replies
            var markers = "tsmtmtm".ToCharArray();
            var lines = new[]{"text",
                      "splitter",
                      ">",
                      "(http://example.com)",
                      ">",
                      "inline  reply",
                      ">"};
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(lines);
        }

        [Test]
        public void test_process_marked_lines_9()
        {
            // inline reply with link not wrapped in paranthesis
            var markers = "tsmtm".ToCharArray();
            var lines = new[]{"text",
                      "splitter",
                      ">",
                      "inline reply with link http://example.com",
                      ">"};
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(lines);
        }

        [Test]
        public void test_process_marked_lines_10()
        {
            // inline reply with link wrapped in paranthesis
            var markers = "tsmtm".ToCharArray();
            var lines = new[]{"text",
                      "splitter",
                      ">",
                      "inline  reply (http://example.com)",
                      ">"};
            Quotations.ProcessMarkedLines(lines, markers).Lines.ShouldBe(lines);
        }

        [Test]
        public void test_preprocess_1()
        {
            var msg = "Hello\n" +
                   "See <http://google.com\n" +
                   "> for more\n" +
                   "information On Nov 30, 2011, at 12:47 PM, Somebody <\n" +
                   "416ffd3258d4d2fa4c85cfa4c44e1721d66e3e8f4\n" +
                   "@example.com>" +
                   "wrote:\n" +
                   "\n" +
        "> Hi";

            // test the link is rewritten
            // "On <date> <person> wrote:" pattern starts from a new line
            var prepared_msg = "Hello\n" +
                            "See @@http://google.com\n" +
                            "@@ for more\n" +
                            "information\n" +
                            " On Nov 30, 2011, at 12:47 PM, Somebody <\n" +
                            "416ffd3258d4d2fa4c85cfa4c44e1721d66e3e8f4\n" +
                            "@example.com>" +
                            "wrote:\n" +
                            "\n" +
                            "> Hi";
            Quotations.Preprocess(msg, "\n").ShouldBe(prepared_msg);
        }

        [Test]
        public void test_preprocess_2()
        {
            var msg = @"
> <http://teemcl.mailgun.org/u/**aD1mZmZiNGU5ODQwMDNkZWZlMTExNm**

> MxNjQ4Y2RmOTNlMCZyPXNlcmdleS5v**YnlraG92JTQwbWFpbGd1bmhxLmNvbS**

> Z0PSUyQSZkPWUwY2U<http://example.org/u/aD1mZmZiNGU5ODQwMDNkZWZlMTExNmMxNjQ4Y>
    ";

            Quotations.Preprocess(msg, "\n").ShouldBe(msg);
        }

        [Test]
        public void test_preprocess_3()
        {
            // "On <date> <person> wrote" shouldn"t be spread across too many lines
            var msg = "Hello\n" +
                      "How are you? On Nov 30, 2011, at 12:47 PM,\n " +
                      "Example <\n" +
                      "416ffd3258d4d2fa4c85cfa4c44e1721d66e3e8f4\n" +
                      "@example.org>" +
                      "wrote:\n" +
                      "\n" +
                      "> Hi";
            Quotations.Preprocess(msg, "\n").ShouldBe(msg);
        }

        [Test]
        public void test_preprocess_4()
        {
            var msg = "Hello On Nov 30, smb wrote:\n" +
                   "Hi\n" +
                   "On Nov 29, smb wrote:\n" +
                   "hi";

            var prepared_msg = "Hello\n" +
                            " On Nov 30, smb wrote:\n" +
                            "Hi\n" +
                            "On Nov 29, smb wrote:\n" +
                            "hi";

            Quotations.Preprocess(msg, "\n").ShouldBe(prepared_msg);
        }

        [Test]
        public void test_preprocess_postprocess_2_links()
        {
            var msg_body = "<http://link1> <http://link2>";
            Quotations.ExtractFromPlain(msg_body).ShouldBe(msg_body);
        }

        private static readonly string StandardRepliesDirectory = "Fixtures/StandardReplies";

        [TestCase("android.eml")]
        [TestCase("aol.eml")]
        [TestCase("apple_mail.eml")]
        [TestCase("comcast.eml")]
        [TestCase("gmail.eml")]
        [TestCase("hotmail.eml")]
        //[TestCase("iphone.eml")] this one fails... and it has the wrong case anyway (hello vs Hello)
        [TestCase("outlook.eml")]
        [TestCase("sparrow.eml")]
        [TestCase("thunderbird.eml")]
        [TestCase("yahoo.eml")]
        public void test_standard_replies(string filename)
        {
            var m = MimeMessage.Load(Path.Combine(StandardRepliesDirectory, filename));
            foreach (var part in m.BodyParts)
            {
                if (part.ContentType.MimeType == "text/plain")
                {
                    var textStream = part.ContentObject.Open();
                    var text = new StreamReader(textStream).ReadToEnd();
                    var strippedText = Quotations.ExtractFromPlain(text);
                    var replyText = "Hello";

                    string replyFile = Path.Combine(StandardRepliesDirectory, Path.GetFileNameWithoutExtension(filename) + "_reply_text");
                    if (File.Exists(replyFile))
                        replyText = File.ReadAllText(replyFile);
                    strippedText.ShouldBe(replyText);
                }
            }
        }
    }
}
