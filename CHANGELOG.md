FluentAssertions Migrator Changelog


<a name="2.0.0"></a>
## [2.0.0](https://github.com/amoerie/fluentassertions-migrator/releases/tag/v2.0.0) (2026-07-14)

### Features

* upgrade to .NET 10, xUnit v3 and Microsoft Testing Platform ([7728ce3](https://github.com/amoerie/fluentassertions-migrator/commit/7728ce351c41bc7c1a776f114e6ba8bc73715277))
* add handlers for inclusive numeric comparisons (`BeGreaterThanOrEqualTo`, `BeLessThanOrEqualTo`, `BePositive`, `BeNegative`), dictionaries (`ContainKey`, `NotContainKey`), regex (`MatchRegex`, `NotMatchRegex`) and collections (`OnlyContain`, `Equal`) ([713d55d](https://github.com/amoerie/fluentassertions-migrator/commit/713d55d3691df7542628484def09a51745038767))
* add clean 1:1 handlers for `NotEqual`, `HaveLength`, `BeNullOrWhiteSpace`, `NotBeNullOrWhiteSpace`, `BeAssignableTo<T>`, `HaveCountGreaterThan` and `HaveCountGreaterThanOrEqualTo` ([b128889](https://github.com/amoerie/fluentassertions-migrator/commit/b12888938741b9fc3eb9a19009fe22dabdfadbb3))

### Bug Fixes

* match nested generic type arguments in the type and exception regexes (e.g. `BeOfType<List<int>>`, `Throw<CustomException<T>>`), which were previously left unmigrated ([b128889](https://github.com/amoerie/fluentassertions-migrator/commit/b12888938741b9fc3eb9a19009fe22dabdfadbb3))
