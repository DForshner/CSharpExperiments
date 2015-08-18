// Example of applying an EQUAL expression to a property selector.

public class A
{
	public string PropA { get; set; }
}

public Expression<Func<T, bool>> FancyPropEquals<T>(string val, Expression<Func<T, string>> selector) {
	var constVal = Expression.Constant(val, typeof(string));

	// {(x.PropA == "foo")}    =            x.PropA        "foo"
	var selectorEquals = Expression.Equal(selector.Body, constVal);

	//                                   {(x.PropA == "foo")}         [ { x } ]
	return Expression.Lambda<Func<T, bool>>(selectorEquals, selector.Parameters);
}

public void Demo()
{
	var foos = new []
	{
		new A { PropA = "foo" },
		new A { PropA = "bar" },
	}.AsQueryable();
	var foo = foos.Where(FancyPropEquals<A>("foo", x => x.PropA));
}