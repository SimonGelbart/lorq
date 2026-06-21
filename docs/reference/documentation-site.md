# Documentation Site

LORQ publishes project documentation from the `main` branch through GitHub Pages.

## Source of truth

Documentation source lives with the project code:

```text
mkdocs.yml
docs/
  README.md
  adr/
  tutorials/
  how-to/
  reference/
  explanation/
  roadmap/
```

The published site is generated output. Do not edit generated HTML by hand.

## URL model

```text
https://simongelbart.github.io/lorq/
https://simongelbart.github.io/lorq/adr/
```

## Main-site integration

The global site at `https://simongelbart.github.io/` links to this project documentation site and can synchronize lightweight metadata from:

```text
docs/project.json
```

The main site should not mirror the complete LORQ documentation tree.

## ADR visibility

Architecture Decision Records remain in:

```text
docs/adr/
```

The ADR index is published as a first-class documentation section.
