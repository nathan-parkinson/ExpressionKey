using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ExpressionKey;

namespace ExpressionKeyApp
{
    public class ExpressionKeyLookupTests
    {
        public ExpressionKeyLookupTests()
        {
            var people = Enumerable.Range(1, 100).Select(x => new Person { PersonId = (int)Math.Ceiling(x / 5.0f), Name = "Person " + x }).ToList();
            var children = Enumerable.Range(1, 100).Select(x => new PersonChild { ParentId = (int)Math.Ceiling(x / 5.0f), Name = "PersonChild " + x, PersonChildId = x }).ToList();

            foreach (var item in people)
            {
                if (item.PersonId > 1)
                {
                    item.ParentId = item.PersonId - 1;
                }
            }

            //TODO have a fall back for this scenario or at leat a graceful way to handle it
            Expression<Func<PersonChild, Person, bool>> unhashableExpr = (pc, p) => p.PersonId < pc.ParentId;
            var lookupImpossible = children.ToExpressionKeyLookup(unhashableExpr);

            Expression<Func<PersonChild, Person, bool>> expr = (pc, p) => p.PersonId + 1 == pc.ParentId + 1 && !(p.Name.First() != pc.Name.First());
            var lookup = children.ToExpressionKeyLookup(expr);


            var matchedItems = children.SetReferences(c => c.Parent, people, expr);
            
            Expression<Func<Person, PersonChild, bool>> expr2 = (p, pc) => pc.ParentId == p.PersonId;
            var lookup2 = people.ToExpressionKeyLookup(expr2);

            Expression<Func<Person, Person, bool>> exprSelf = (p, c) => p.PersonId == c.ParentId;
            var lookupSelf = people.ToExpressionKeyLookup(exprSelf);

            foreach (var person in people)
            {
                Debug.WriteLine("--------------------------------------------");
                Debug.WriteLine("--------------------------------------------");
                Debug.Write(JsonConvert.SerializeObject(person, Formatting.Indented));
                foreach(var result in lookup.GetMatches(person))
                {
                    Debug.WriteLine("|||||||||||||||||||||||||||");
                    Debug.Write(JsonConvert.SerializeObject(result, Formatting.Indented));
                    Debug.WriteLine("Reverse lookup");
                    foreach(var reverse in lookup2.GetMatches(result).Distinct())
                    {
                        Debug.WriteLine("^^^^^^^^^^^^^^^^^^^");
                        Debug.Write(JsonConvert.SerializeObject(reverse, Formatting.Indented));
                    }
                }
            }
        }
    }

    public class Person
    {
        public int PersonId { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; }

        public List<PersonChild> Children { get; set; } = new List<PersonChild>();
    }

    public class PersonChild
    {
        public int PersonChildId { get; set; }
        public int ParentId { get; set; }
        public Person Parent { get; set; }
        public List<Person> Parents { get; set; }
        public string Name { get; set; }
    }
}
