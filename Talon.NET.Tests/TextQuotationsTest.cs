using System;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace Talon.Tests
{
    public class TextQuotationsTest
    {
        [Test]
        public void test_too_many_lines()
        {
            var msg_body = "test message" + String.Join("\n", Enumerable.Repeat("line", 1005)) + @"
            
            //-----Original Message-----

            Test";

            Quotations.ExtractFromPlain(msg_body).ShouldBe(msg_body);
        }

        [Test]
        public void test_pattern_on_date_somebody_wrote()
        {
            var msg_body = @"Test reply

On 11-Apr-2011, at 6:54 PM, Roman Tkachenko <romant@example.com> wrote:

>
> Test
>
> Roman";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Test reply");
        }

        [Test]
        public void test_pattern_on_date_somebody_wrote_date_with_slashes()
        {
            var msg_body = @"Test reply

On 04/19/2011 07:10 AM, Roman Tkachenko wrote:

>
> Test.
>
> Roman";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Test reply");
        }

        [Test]
        public void test_pattern_on_date_somebody_wrote_allows_space_in_front()
        {
            var msg_body = @"Thanks Thanmai
 On Mar 8, 2012 9:59 AM, ""Example.com"" <
r+7f1b094ceb90e18cca93d53d3703feae@example.com> wrote:


>**
>  Blah-blah-blah";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Thanks Thanmai");
        }

        [Test]
        public void test_pattern_on_date_somebody_sent()
        {
            var msg_body = @"Test reply

On 11-Apr-2011, at 6:54 PM, Roman Tkachenko <romant@example.com> sent:

>
> Test
>
> Roman";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Test reply");
        }

        [Test]
        public void test_line_starts_with_on()
        {
            var msg_body = @"Blah-blah-blah
On blah-blah-blah";
            Quotations.ExtractFromPlain(msg_body).ShouldBe(msg_body);
        }

        [Test]
        public void test_reply_and_quotation_splitter_share_line()
        {
            // reply lines and 'On <date> <person> wrote:' splitter pattern
            //# are on the same line
            var msg_body = @"reply On Wed, Apr 4, 2012 at 3:59 PM, bob@example.com wrote:
> Hi";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("reply");

            // test pattern '--- On <date> <person> wrote:' with reply text on
            // the same line
            msg_body = @"reply--- On Wed, Apr 4, 2012 at 3:59 PM, me@domain.com wrote:
> Hi";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("reply");

            // test pattern '--- On <date> <person> wrote:' with reply text containing
            // '-' symbol
            msg_body = @"reply
bla-bla - bla--- On Wed, Apr 4, 2012 at 3:59 PM, me@domain.com wrote:
> Hi";
            var reply = @"reply
bla-bla - bla";

            Quotations.ExtractFromPlain(msg_body).ShouldBe(reply);
        }

        [Test]
        public void test_pattern_original_message()
        {
            var msg_body = @"Test reply

-----Original Message-----

Test";

            Quotations.ExtractFromPlain(msg_body).ShouldBe("Test reply");

            msg_body = @"Test reply

 -----Original Message-----

Test";

            Quotations.ExtractFromPlain(msg_body).ShouldBe("Test reply");
        }

        [Test]
        public void test_reply_after_quotations()
        {
            var msg_body = @"On 04/19/2011 07:10 AM, Roman Tkachenko wrote:

>
> Test
Test reply";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Test reply");
        }

        [Test]
        public void test_reply_wraps_quotations()
        {
            var msg_body = @"Test reply

On 04/19/2011 07:10 AM, Roman Tkachenko wrote:

>
> Test

Regards, Roman";

            var reply = @"Test reply

Regards, Roman";

            Quotations.ExtractFromPlain(msg_body).ShouldBe(reply);
        }

        [Test]
        public void test_reply_wraps_nested_quotations()
        {
            var msg_body = @"Test reply
On 04/19/2011 07:10 AM, Roman Tkachenko wrote:

>Test test
>On 04/19/2011 07:10 AM, Roman Tkachenko wrote:
>
>>
>> Test.
>>
>> Roman

Regards, Roman";

            var reply = @"Test reply
Regards, Roman";
            Quotations.ExtractFromPlain(msg_body).ShouldBe(reply);
        }

        [Test]
        public void test_quotation_separator_takes_2_lines()
        {
            var msg_body = @"Test reply

On Fri, May 6, 2011 at 6:03 PM, Roman Tkachenko from Hacker News
<roman@public voidinebox.com> wrote:

> Test.
>
> Roman

Regards, Roman";

            var reply = @"Test reply

Regards, Roman";
            Quotations.ExtractFromPlain(msg_body).ShouldBe(reply);
        }

        [Test]
        public void test_quotation_separator_takes_3_lines()
        {
            var msg_body = @"Test reply

On Nov 30, 2011, at 12:47 PM, Somebody <
416ffd3258d4d2fa4c85cfa4c44e1721d66e3e8f4@somebody.domain.com>
wrote:

Test message
";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Test reply");
        }

        [Test]
        public void test_short_quotation()
        {
            var msg_body = @"Hi

On 04/19/2011 07:10 AM, Roman Tkachenko wrote:

> Hello";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Hi");
        }

        [Test]
        public void test_pattern_date_email_with_unicode()
        {
            var msg_body = @"Replying ok
2011/4/7 Nathan \xd0\xb8ova <support@example.com>

>  Cool beans, scro";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Replying ok");
        }

        [Test]
        public void test_pattern_from_block()
        {
            var msg_body = @"Allo! Follow up MIME!

From: somebody@example.com
Sent: March-19-11 5:42 PM
To: Somebody
Subject: The manager has commented on your Loop

Blah-blah-blah
";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Allo! Follow up MIME!");
        }

        [Test]
        public void test_quotation_marker_false_positive()
        {
            var msg_body = @"Visit us now for assistance...
>>> >>>  http://www.domain.com <<<
Visit our site by clicking the link above";
            Quotations.ExtractFromPlain(msg_body).ShouldBe(msg_body);
        }

        [Test]
        public void test_link_closed_with_quotation_marker_on_new_line()
        {
            var msg_body = @"8.45am-1pm

From: somebody@example.com

<http://email.example.com/c/dHJhY2tpbmdfY29kZT1mMDdjYzBmNzM1ZjYzMGIxNT
>  <bob@example.com <mailto:bob@example.com> >

Requester: ";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("8.45am-1pm");
        }

        [Test]
        public void test_link_breaks_quotation_markers_sequence()
        {
            // link starts and ends on the same line
            var msg_body = @"Blah

On Thursday, October 25, 2012 at 3:03 PM, life is short. on Bob wrote:

>
> Post a response by replying to this email
>
 (http://example.com/c/YzOTYzMmE) >
> life is short. (http://example.com/c/YzMmE)
> ";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Blah");

            // link starts after some text on one line and ends on another
            msg_body = @"Blah

On Monday, 24 September, 2012 at 3:46 PM, bob wrote:

> [Ticket #50] test from bob
>
> View ticket (http://example.com/action
_nonce=3dd518)
>
";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Blah");
        }

        [Test]
        public void test_from_block_starts_with_date()
        {
            var msg_body = @"Blah

Date: Wed, 16 May 2012 00:15:02 -0600
To: klizhentas@example.com";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Blah");
        }

        [Test]
        public void test_bold_from_block()
        {
            var msg_body = @"Hi

  *From:* bob@example.com [mailto:
  bob@example.com]
  *Sent:* Wednesday, June 27, 2012 3:05 PM
  *To:* travis@example.com
  *Subject:* Hello

";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Hi");
        }

        [Test]
        public void test_weird_date_format_in_date_block()
        {
            var msg_body = @"Blah
Date: Fri=2C 28 Sep 2012 10:55:48 +0000
From: tickets@example.com
To: bob@example.com
Subject: [Ticket #8] Test

";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Blah");
        }

        [Test]
        public void test_dont_parse_quotations_for_forwarded_messages()
        {
            var msg_body = @"FYI

---------- Forwarded message ----------
From: bob@example.com
Date: Tue, Sep 4, 2012 at 1:35 PM
Subject: Two
line subject
To: rob@example.com

Text";
            Quotations.ExtractFromPlain(msg_body).ShouldBe(msg_body);
        }

        [Test]
        public void test_forwarded_message_in_quotations()
        {
            var msg_body = @"Blah

-----Original Message-----

FYI

---------- Forwarded message ----------
From: bob@example.com
Date: Tue, Sep 4, 2012 at 1:35 PM
Subject: Two
line subject
To: rob@example.com

";
            Quotations.ExtractFromPlain(msg_body).ShouldBe("Blah");
        }
    }
}
