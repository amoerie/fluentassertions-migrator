FluentAssertions Migrator Changelog


<a name="2.2.0"></a>
## [2.2.0](https://github.com/amoerie/fluentassertions-migrator/compare/v2.1.1...v2.2.0) (2026-07-14)

### Features

* preserve async on unwrapped Invoking/Awaiting and migrate ContainSingle().Which ([7e25e7c](https://github.com/amoerie/fluentassertions-migrator/commit/7e25e7c68f974ccd9ec222c4919ba67cee480c87))

<a name="2.1.1"></a>
## [2.1.1](https://github.com/amoerie/fluentassertions-migrator/compare/v2.1.0...v2.1.1) (2026-07-14)

### Bug Fixes

* match fluent chains that span multiple lines ([b630679](https://github.com/amoerie/fluentassertions-migrator/commit/b6306790970b2626407361436fe97d24ea5dca33))

<a name="2.1.0"></a>
## [2.1.0](https://github.com/amoerie/fluentassertions-migrator/compare/v2.0.0...v2.1.0) (2026-07-14)

### Features

* migrate exception-detail chains WithMessage and WithParameterName ([92c8915](https://github.com/amoerie/fluentassertions-migrator/commit/92c8915846fe7153956314f7fb991de9b35711b7))
* unwrap Invoking/Awaiting into plain lambdas for throw assertions ([c0adbd2](https://github.com/amoerie/fluentassertions-migrator/commit/c0adbd29d5abbc4036a6dc48fbbcd58bfaa14600))

<a name="2.0.0"></a>
## [2.0.0](https://github.com/amoerie/fluentassertions-migrator/releases/tag/v2.0.0) (2026-07-14)

### Features

* upgrade to .NET 10, xUnit v3 and Microsoft Testing Platform ([7728ce3](https://github.com/amoerie/fluentassertions-migrator/commit/7728ce351c41bc7c1a776f114e6ba8bc73715277))
* add handlers for inclusive numeric comparisons (`BeGreaterThanOrEqualTo`, `BeLessThanOrEqualTo`, `BePositive`, `BeNegative`), dictionaries (`ContainKey`, `NotContainKey`), regex (`MatchRegex`, `NotMatchRegex`) and collections (`OnlyContain`, `Equal`) ([713d55d](https://github.com/amoerie/fluentassertions-migrator/commit/713d55d3691df7542628484def09a51745038767))
* add clean 1:1 handlers for `NotEqual`, `HaveLength`, `BeNullOrWhiteSpace`, `NotBeNullOrWhiteSpace`, `BeAssignableTo<T>`, `HaveCountGreaterThan` and `HaveCountGreaterThanOrEqualTo` ([b128889](https://github.com/amoerie/fluentassertions-migrator/commit/b12888938741b9fc3eb9a19009fe22dabdfadbb3))

### Bug Fixes

* match nested generic type arguments in the type and exception regexes (e.g. `BeOfType<List<int>>`, `Throw<CustomException<T>>`), which were previously left unmigrated ([b128889](https://github.com/amoerie/fluentassertions-migrator/commit/b12888938741b9fc3eb9a19009fe22dabdfadbb3))
