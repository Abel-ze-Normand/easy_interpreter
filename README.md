# easy_interpreter
EasyScript Interpreter

# Grammar
``` grammar
Program: 
    StatementList
Statement: 
    IfClause    | 
    WhileClause | 
    Assignment ';'
StatementList: 
    Statement*
Assignment: 
    Expression ('=' Expression)?
IfClause: 
    'if''('Expression')''{'StatementList'}'
WhileClause: 
    'while''('Expression')''{'StatementList'}'
Identifier: 
    \w[\w\d_]*
Number: 
    [0-9]+
ARG: 
    Identifier | 
    Number
Expression: 
    Comparison
Comparison: 
    Sum(('=='|'!='|'<'|'>'|'<='|'>=')Sum)?
Sum: 
    Product(('+'|'-')Product)*
Product: 
    Primary(('*'|'/')Primary)*
Primary: 
    '('Comparison')' FuncCallArguments? | 
    ARG FuncCall?                       |
    - Primary # unary minus
FuncCallArguments: 
    '(' Expression (',' Expression)* ')'
```

# Example 
``` ex
a = 1 + 2; //3
b = a + 1; //4
c = a == b; //false
c = 1;
c = true;
if (c == true){
	print(a);
}
while (a < b + 10){
	print(a);
	a = a + 1;
}
```
