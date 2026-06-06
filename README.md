## GraphQL Coffee Beanery

#### “Why GraphQL and Dapper with GraphQL Coffee Beanery and what does better than existing tools?

Explore the unique approach of using dapper for performance and direct GraphQL operation translations into Raw SQL, providing out of the box features without adding any code, and giving full customisation and integrations based on every project needs.

#### “Graphql + C# + Dapper + PostgreSQL - Example”
#### “Coffee Beanery + Entity Framework GraphQL”
#### “GraphQL Dapper C# 
#### “HotChocolate + Dapper Example"

Coffee beanery is a dynamic parser from GraphQL queries into raw SQL queries; the translation happens on the fly and all the features are available out of the box. 

It only requires mappings between models and entities and a few annotations to signal the framework about the relationship between models or entities.

Also, the feature to have the means for using business transactions and add custom business code within the api. Makes a unique opportunity to do any integration possible.

“Actively developed. Currently a reference implementation and experimentation platform, it might be shipped as a Nuget package once it reaches maturity. 

Any type of contribution and support will be extremely welcome. From proposals, feedback, or any other content that can help out to reach a a wider audience and maturity.”

#### Current focus is to support Graph Data Models and porting the last few remaining features

Running example

1. Clone repository
2. Run entity framework migrations
3. Compile and run api project
3. Use nitro IDE to create any type of graphql operation.
4. Validate data persistance and query result.

The following libraries are used to achieve all the features listed below:

- Dapper
- Hot Chocolate
- Entity Framework
- PostgreSQL
- FasterKV

## Current Features

- Configuration based
- No N+1 problem since the entire query/mutation is batched and materialized by the database engine
- Hability to add any additional business logic or integration within the GraphQL API project
- Custom and complex mapping between data entities and domain models
- Allows subgraph mutations and queries using the same endpoint and wrapper object
- Node types are translated into Left joins between entities.
- Edge types are translated into joins between entities.
- Paging support out of the box
- Filtering support out of the box
- Sorting support out of the box

## Customizable Features

- Granular access by table/columns based on token-claims
- Data and column validations
- Query cache can be customized in multiple layers
- Query result handling can be fully customized

## Documentation in progress - check the [example](https://github.com/CristianBarragan/GraphQL-Coffee-Beanery/tree/main/example/HotChocolateCoffeeBeanery), contains several types of mappings, it only needs a mapping setup between entities and models.

## Tests

<img src="https://github.com/CristianBarragan/Coffee-Beanery/blob/main/example/HotChocolateCoffeeBeanery/Test/Test_Results.png" alt="Test_Results" height="60%" width="100%">

### [Buy me a Coffee ☕]
*I would love a 100% colombian coffee!*

<a href="https://www.buymeacoffee.com/cristianbarragan" target="_blank">
<img src="https://cdn.buymeacoffee.com/buttons/default-orange.png" alt="Buy Me A Coffee" height="41" width="174">
</a>
