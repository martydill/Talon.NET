using System;
using NUnit.Framework;
using Shouldly;

namespace Talon.Tests
{
    internal class BruteforceTest
    {
        [Test]
        public void test_empty_body()
        {
            Bruteforce.ExtractSignature("").ShouldBe(new Tuple<string, string>("", null));
        }

        [Test]
        public void test_no_signature()
        {
            var msgBody = "Hey man!";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>(msgBody, null));
        }

        [Test]
        public void test_signature_only()
        {
            var msgBody = "--\nRoman";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>(msgBody, null));
        }

        [Test]
        public void test_signature_separated_by_dashes()
        {
            var msgBody = @"Hey man! How r u?
---
Roman";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Hey man! How r u?", "---\r\nRoman"));
        }

        [Test]
        public void test_signature_separated_by_dashes_2()
        {
            var msgBody = @"Hey!
-roman";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>("Hey!", "-roman"));
        }

        [Test]
        public void test_signature_separated_by_dashes_3()
        {
            var msgBody = @"Hey!

- roman";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>("Hey!", "- roman"));
        }

        [Test]
        public void test_signature_separated_by_dashes_4()
        {
            var msgBody = @"Wow. Awesome!
--
Bob Smith";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>("Wow. Awesome!", "--\r\nBob Smith"));
        }

        [Test]
        public void test_signature_words()
        {
            var msgBody = @"Hey!

Thanks!
Roman";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>("Hey!", "Thanks!\r\nRoman"));
        }

        [Test]
        public void test_signature_words_2()
        {
            var msgBody = @"Hey!
--
Best regards,

Roman";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Hey!", "--\r\nBest regards,\r\n\r\nRoman"));
        }

        [Test]
        public void test_signature_words_3()
        {
            var msgBody = @"Hey!
--
--
Regards,
Roman";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Hey!", "--\r\n--\r\nRegards,\r\nRoman"));
        }

        [Test]
        public void test_iphone_signature()
        {
            var msgBody = @"Hey!

Sent from my iPhone!";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>("Hey!", "Sent from my iPhone!"));
        }

        [Test]
        public void test_mailbox_for_iphone_signature()
        {
            var msgBody = @"Blah
Sent from Mailbox for iPhone";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Blah", "Sent from Mailbox for iPhone"));
        }


        [Test]
        public void test_line_starts_with_signature_word()
        {
            var msgBody = @"Hey man!
Thanks for your attention.
--
Thanks!
Roman";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Hey man!\r\nThanks for your attention.", "--\r\nThanks!\r\nRoman"));
        }

        [Test]
        public void test_line_starts_with_dashes()
        {
            var msgBody = @"Hey man!
Look at this:

--> one
--> two
--
Roman";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Hey man!\r\nLook at this:\r\n\r\n--> one\r\n--> two", "--\r\nRoman"));
        }

        [TestCase("-Lev.")]
        [TestCase("Thanks!")]
        [TestCase("Cheers,")]
        public void test_blank_lines_inside_signature(string test)
        {
            var msgBody = @"Blah.

" + test + @"

Sent from my HTC smartphone!";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Blah.", test + "\r\n\r\nSent from my HTC smartphone!"));
        }

        [Test]
        public void test_blank_lines_inside_signature_2()
        {
            var msgBody = @"Blah
--

John Doe";
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>("Blah", "--\r\n\r\nJohn Doe"));
        }

        [Test]
        public void test_blackberry_signature()
        {
            var msgBody = @"Heeyyoooo.
Sent wirelessly from my BlackBerry device on the Bell network.
Envoyé sans fil par mon terminal mobile BlackBerry sur le réseau de Bell.";

            var sig = msgBody.Substring("Heeyyoooo.".Length, msgBody.Length - "Heeyyoooo.".Length);
            Bruteforce.ExtractSignature(msgBody).ShouldBe(new Tuple<string, string>("Heeyyoooo.", sig.Trim()));
        }

        [Test]
        public void test_blackberry_signature_2()
        {
            var msgBody = @"Blah
Enviado desde mi oficina mÃ³vil BlackBerryÂ® de Telcel";

            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Blah", "Enviado desde mi oficina mÃ³vil BlackBerryÂ® de Telcel"));
        }

        //@patch.object(Bruteforce, "get_delimiter", Mock(side_effect=Exception()))

        //        [Test]
        //        public void test_crash_in_extract_signature()
        //        {
        //            var msgBody = @"Hey!
        //-roman";
        //            eq_((msgBody, None), Bruteforce.extract_signature(msgBody).ShouldBe(new Tuple<string, string>()

        //        }

        [Test]
        public void test_signature_cant_start_from_first_line()
        {
            var msgBody = @"Thanks,

Blah

regards

John Doe";
            Bruteforce.ExtractSignature(msgBody)
                .ShouldBe(new Tuple<string, string>("Thanks,\r\n\r\nBlah", "regards\r\n\r\nJohn Doe"));
        }

        //@patch.object(Bruteforce, "SIGNATURE_MAX_LINES", 2)
        [Test]
        public void test_signature_max_lines_ignores_empty_lines()
        {
            using (new SignatureMaxLinesScope(2))
            {
                var msgBody = @"Thanks,
Blah

regards


John Doe";
                Bruteforce.ExtractSignature(msgBody)
                    .ShouldBe(new Tuple<string, string>("Thanks,\r\nBlah", "regards\r\n\r\n\r\nJohn Doe"));
            }
        }

        [Test]
        public void test_get_signature_candidate()
        {
            // if there aren"t at least 2 non-empty lines there should be no signature
            foreach (var lines in new[] { new string[] { }, new[] { "" }, new[] { "", "" }, new[] { "abc" } })
                Bruteforce.GetSignatureCandidate(lines).ShouldBeEmpty();
        }

        [Test]
        public void test_get_signature_candidate_2()
        {
            // first line never included
            var lines = new[] { "text", "signature" };
            Bruteforce.GetSignatureCandidate(lines).ShouldBe(new[] { "signature" });
        }

        [Test]
        public void test_get_signature_candidate_3()
        {
            // test when message is shorter then SIGNATURE_MAX_LINES
            using (new SignatureMaxLinesScope(3))
            {
                var lines = new[] { "text", "", "", "signature" };
                Bruteforce.GetSignatureCandidate(lines).ShouldBe(new[] { "signature" });
            }
        }

        [Test]
        public void test_get_signature_candidate_4()
        {
            // test when message is longer then the SIGNATURE_MAX_LINES
            using (new SignatureMaxLinesScope(2))
            {
                var lines = new[] { "text1", "text2", "signature1", "", "signature2" };
                Bruteforce.GetSignatureCandidate(lines).ShouldBe(new[] { "signature1", "", "signature2" });
            }
        }

        [Test]
        public void test_get_signature_candidate_5()
        {
            // test long lines not encluded
            using (new TooLongSignatureLineScope(3))
            {
                var lines = new[] { "BR,", "long", "Bob" };
                Bruteforce.GetSignatureCandidate(lines).ShouldBe(new[] { "Bob" });
            }
        }

        [Test]
        public void test_get_signature_candidate_6()
        {
            // test list (with dashes as bullet points) not included
            var lines = new[] { "List:,", "- item 1", "- item 2", "--", "Bob" };
            Bruteforce.GetSignatureCandidate(lines).ShouldBe(new[] { "--", "Bob" });
        }

        [Test]
        public void test_mark_candidate_indexes()
        {
            using (new TooLongSignatureLineScope(3))
            {
                // spaces are not considered when checking line length
                Bruteforce.MarkCandidateIndexes(
                    new[] { "BR,  ", "long", "Bob" },
                    new[] { 0, 1, 2 }).ShouldBe("clc".ToCharArray());
            }
        }

        [Test]
        public void test_mark_candidate_indexes_2()
        {
            using (new TooLongSignatureLineScope(3))
            {
                // only candidate lines are marked
                // if line has only dashes it"s a candidate line
                Bruteforce.MarkCandidateIndexes(
                    new[] { "-", "long", "-", "- i", "Bob" },
                    new[] { 0, 2, 3, 4 }).ShouldBe("ccdc".ToCharArray());
            }
        }

        [Test]
        public void test_process_marked_candidate_indexes()
        {
            Bruteforce.ProcessMarkedCandidateIndexes(
                new[] { 2, 13, 15 }, "dcc".ToCharArray()).ShouldBe(new[] { 2, 13, 15 });
        }

        [Test]
        public void test_process_marked_candidate_indexes_2()
        {
            Bruteforce.ProcessMarkedCandidateIndexes(
                new[] { 2, 13, 15 }, "ddc".ToCharArray()).ShouldBe(new[] { 15 });
        }

        [Test]
        public void test_process_marked_candidate_indexes_3()
        {
            Bruteforce.ProcessMarkedCandidateIndexes(
                new[] { 13, 15 }, "cc".ToCharArray()).ShouldBe(new[] { 13, 15 });
        }

        [Test]
        public void test_process_marked_candidate_indexes_4()
        {
            Bruteforce.ProcessMarkedCandidateIndexes(
                new[] { 15 }, "lc".ToCharArray()).ShouldBe(new[] { 15 });
        }

        [Test]
        public void test_process_marked_candidate_indexes_5()
        {
            Bruteforce.ProcessMarkedCandidateIndexes(
                new[] { 13, 15 }, "ld".ToCharArray()).ShouldBe(new[] { 15 });
        }
    }

    internal class SignatureMaxLinesScope : IDisposable
    {
        private readonly int _old;

        public SignatureMaxLinesScope(int @new)
        {
            _old = Bruteforce.SignatureMaxLines;
            Bruteforce.SignatureMaxLines = @new;
        }

        public void Dispose()
        {
            Bruteforce.SignatureMaxLines = _old;
        }
    }

    internal class TooLongSignatureLineScope : IDisposable
    {
        private readonly int _old;

        public TooLongSignatureLineScope(int @new)
        {
            _old = Bruteforce.TooLongSignatureLine;
            Bruteforce.TooLongSignatureLine = @new;
        }

        public void Dispose()
        {
            Bruteforce.TooLongSignatureLine = _old;
        }
    }
}
