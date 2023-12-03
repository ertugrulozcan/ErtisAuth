using System.Diagnostics.CodeAnalysis;
using ErtisAuth.Core.Models.Roles;
using NUnit.Framework;

namespace ErtisAuth.Tests.Shared.Core.AccessControl
{
	[TestFixture]
	public class RbacTests
	{
		#region Methods

		[Test]
		public void RbacEqualityTest()
		{
			var rbac1 = new Rbac(new RbacSegment("subject"), new RbacSegment("resource"), new RbacSegment("action"), new RbacSegment("object"));
			var rbac2 = new Rbac(new RbacSegment("subject"), new RbacSegment("resource"), new RbacSegment("action"), new RbacSegment("object"));
			var rbac3 = new Rbac(new RbacSegment("subject"), new RbacSegment("resource"), new RbacSegment("action"), new RbacSegment("other"));

			Assert.That(rbac1 == rbac2);
			Assert.That(rbac1 != rbac3);
		}
		
		[Test]
		[SuppressMessage("ReSharper", "ExpressionIsAlwaysNull")]
		public void RbacNullValuesEqualityTest()
		{
			var rbac1 = new Rbac(new RbacSegment("subject"), new RbacSegment("resource"), new RbacSegment("action"), new RbacSegment("object"));
			Rbac rbac2 = null;
			Rbac rbac3 = null;

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			Assert.That(rbac1 != rbac2);
			
			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			Assert.That(rbac2 == rbac3);
		}
		
		[Test]
		public void RbacSegmentEqualityTest()
		{
			var segment1 = new RbacSegment("0000");
			var segment2 = new RbacSegment("0000");
			var segment3 = new RbacSegment("0001");

			Assert.That(segment1 == segment2);
			Assert.That(segment1 != segment3);
		}
		
		[Test]
		public void RbacParseTest()
		{
			var rbac1 = Rbac.Parse("subject.resource.action.*");
			Assert.That("subject" == rbac1.Subject);
			Assert.That("resource" == rbac1.Resource);
			Assert.That("action" == rbac1.Action);
			Assert.That(RbacSegment.All == rbac1.Object);
			
			var rbac2 = Rbac.Parse("*.resource.action.*");
			Assert.That(RbacSegment.All == rbac2.Subject);
			Assert.That("resource" == rbac2.Resource);
			Assert.That("action" == rbac2.Action);
			Assert.That(RbacSegment.All == rbac2.Object);
			
			var rbac3 = Rbac.Parse("*.resource.action.object");
			Assert.That(RbacSegment.All == rbac3.Subject);
			Assert.That("resource" == rbac3.Resource);
			Assert.That("action" == rbac3.Action);
			Assert.That("object" == rbac3.Object);
		}
		
		[Test]
		public void RbacEquality_WithOperator_AllSegmentsSame_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.*");
			var rbac2 = Rbac.Parse("*.*.*.*");
			Assert.That(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithOperator_AllSegmentsSame_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.users.read.*");
			var rbac2 = Rbac.Parse("*.users.read.*");
			Assert.That(rbac1 == rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_AllSegmentsSame_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.*");
			var rbac2 = Rbac.Parse("*.*.*.*");
			Assert.That(rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithEquals_AllSegmentsSame_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.users.read.*");
			var rbac2 = Rbac.Parse("*.users.read.*");
			Assert.That(rbac1.Equals(rbac2));
		}

		[Test]
		public void RbacEquality_WithOperator_SubjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("987.*.*.*");
			var rbac2 = Rbac.Parse("123.*.*.*");
			Assert.That(rbac1 != rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_SubjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("987.*.*.*");
			var rbac2 = Rbac.Parse("123.*.*.*");
			Assert.That(!rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithOperator_ResourceSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.123.*.*");
			var rbac2 = Rbac.Parse("*.987.*.*");
			Assert.That(rbac1 != rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_ResourceSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.123.*.*");
			var rbac2 = Rbac.Parse("*.987.*.*");
			Assert.That(!rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithOperator_ActionSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.123.*");
			var rbac2 = Rbac.Parse("*.*.987.*");
			Assert.That(rbac1 != rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_ActionSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.123.*");
			var rbac2 = Rbac.Parse("*.*.987.*");
			Assert.That(!rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_WithOperator_ObjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.That(rbac1 != rbac2);
		}
		
		[Test]
		public void RbacEquality_WithEquals_ObjectSegmentsDifferent_ReturnFalse_Test()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.That(!rbac1.Equals(rbac2));
		}
		
		[Test]
		public void RbacEquality_NullCheck_WithNotNull_ReturnTrue_Test()
		{
			var rbac = Rbac.Parse("*.*.*.123");
			Assert.That(rbac != null);
		}
		
		[Test]
		public void RbacEquality_NullCheck_WithNotNull_ReturnFalse_Test()
		{
			var rbac = Rbac.Parse("*.*.*.123");
			Assert.That(rbac != null);
		}
		
		[Test]
		public void RbacSegmentEquality_AllSegmentCheck_ReturnTrue_Test()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			Assert.That(rbac1.Resource == RbacSegment.All);
		}
		
		[Test]
		public void RbacSegmentEquality_WithOperator_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.That(rbac1.Action == rbac2.Action);
		}
		
		[Test]
		public void RbacSegmentEquality_WithEquals_ReturnTrue_Test1()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.That(rbac1.Action.Equals(rbac2.Action));
		}
		
		[Test]
		public void RbacSegmentEquality_WithOperator_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.123");
			Assert.That(rbac1.Object == rbac2.Object);
		}
		
		[Test]
		public void RbacSegmentEquality_WithEquals_ReturnTrue_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.123");
			Assert.That(rbac1.Object.Equals(rbac2.Object));
		}
		
		[Test]
		public void RbacSegmentEquality_WithOperator_ReturnFalse_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.That(rbac1.Object != rbac2.Object);
		}
		
		[Test]
		public void RbacSegmentEquality_WithEquals_ReturnFalse_Test2()
		{
			var rbac1 = Rbac.Parse("*.*.*.123");
			var rbac2 = Rbac.Parse("*.*.*.987");
			Assert.That(!rbac1.Object.Equals(rbac2.Object));
		}
		
		[Test]
		public void Rbac_ToString_Test()
		{
			var rbac = Rbac.Parse("123.users.create.987");
			Assert.That("123.users.create.987" == rbac.ToString());
		}
		
		#endregion
	}
}