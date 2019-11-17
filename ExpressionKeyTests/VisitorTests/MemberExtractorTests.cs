using ExpressionKey.Visitors;
using NUnit.Framework;
using System;
using System.Linq.Expressions;

namespace ExpressionKeyTests
{
    public class MemberExtractorTests
    {
        [Test]
        public void ExtractSingleMemberFailWhenNoMembersExistTest()
        {
            bool exceptionThrown = false;
            Expression<Func<TestClass, int>> expr = p => 12;
            try
            {
                MemberExtractor.ExtractSingleMember(expr);
            }
            catch(ArgumentNullException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }



        [Test]
        public void ExtractSingleMemberFailWhenMulitpleMembersExistTest()
        {
            bool exceptionThrown = false;
            Expression<Func<TestClass, int>> expr = p => p.Id + p.ParentId.Value;
            try
            {
                MemberExtractor.ExtractSingleMember(expr);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown);
        }

        [Test]
        public void ExtractSingleMemberSucceedsWhenOneMemberExistsTest()
        {
            Expression<Func<TestClass, int>> expr = p => p.Id;            
            var memberExpression = MemberExtractor.ExtractSingleMember(expr);

            Assert.AreEqual(memberExpression.Member, typeof(TestClass).GetProperty(nameof(TestClass.Id)));
        }



        public class TestClass
        {
            public int Id { get; set; }
            public int? ParentId { get; set; }
            public string Name { get; set; }
        }
    }
}