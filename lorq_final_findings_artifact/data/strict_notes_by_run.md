# Strict notes by run

## admin-product-edit-field

### graphify — 83.0

Best strict answer for this case: complete and practical, but still not a near-perfect artifact because it is broad and caveats are implicit.

Strengths: Very complete MVC/admin-field map with good file evidence and supporting implementation path.

Weaknesses: Over-complete for the prompt; could be tighter and explicitly mark optional vs mandatory files.

### base — 80.0

Strong implementation map, but stricter grading penalizes breadth, shortened paths, and lack of crisp source-verified next-step sequencing.

Strengths: Covers controller/model/factory/view/domain/persistence path and gives usable implementation guidance.

Weaknesses: Too broad for candidate-path task; deterministic source path formatting issues; not enough separation between confirmed files and inferred follow-ups.

### graphify+crg — 76.0

Good but not efficient or crisp: it maps the area well, but strict grading penalizes over-breadth and some plausible-but-not-proven migration/version guidance.

Strengths: Broad end-to-end coverage with controller/model/factory/view/persistence guidance.

Weaknesses: Too exhaustive; some guidance reads inferred rather than source-demonstrated; not better than Graphify alone.

### crg — 70.0

Usable but clearly weaker than base/Graphify under stricter grading: it covers the main path, but with thinner implementation specificity.

Strengths: Finds required files and most adjacent components.

Weaknesses: Less concrete on existing field examples, UI binding, and exact implementation sequence; weaker developer handoff.

## admin-permissions

### graphify — 84.0

Strongest strict answer for permissions: clear architecture and permission-flow evidence, though still not flawless as a step-by-step change guide.

Strengths: Excellent coverage of authorization attributes, OR semantics, permission service/records, and registration flow.

Weaknesses: Less explicit on admin UI management and concrete add-new-permission checklist.

### crg — 79.0

Very usable but not presentation-perfect: broad structural coverage, but less crisp as a workflow handoff.

Strengths: Broad permission structure, plugin/admin notes, authorization behavior, service details.

Weaknesses: Verbose; weaker concrete “do this next” guidance; risks overwhelming a daily-workflow user.

### base — 78.0

Solid answer, but stricter grading lowers it because it is explanatory rather than a concise implementation-ready path.

Strengths: Explains admin authorization layers and key permission components.

Weaknesses: Could better prioritize exact files/actions for adding/checking a permission; somewhat verbose.

### graphify+crg — 68.0

Acceptable but noticeably behind: main layers are present, but it lacks enough concrete examples and implementation guidance for a high score.

Strengths: Covers attributes, service, records, startup, and management areas.

Weaknesses: Too generic in places; less evidence and less actionable than other modes.

## inactive-customers-csv

### graphify — 83.0

Best strict answer: gives a strong everyday export implementation path with useful nuance, but still leaves a few UI/localization details underdeveloped.

Strengths: Clear path through controller, search model, factory/service/export manager; good inactive-definition discussion.

Weaknesses: Could be more explicit on button placement/localization and exact CSV formatter reuse.

### base — 79.0

Strong and usable, but strict scoring penalizes length and incomplete UI/localization specificity.

Strengths: Good controller/search model/service/export path; useful inactivity semantics.

Weaknesses: Long; should more clearly distinguish query/filter/export/UI changes and exact CSV precedent.

### crg — 75.0

Usable but not as strong as base/Graphify: good semantics, thinner controller/UI details.

Strengths: Good inactivity semantics and service/export coverage.

Weaknesses: Less complete on UI binding, controller action details, and implementation sequence.

### graphify+crg — 72.0

Usable concise map, but strict grading penalizes thinner source evidence and less complete action detail.

Strengths: Good integration-point table and service/export/resource mentions.

Weaknesses: Less exhaustive evidence; less concrete about existing export action and where exactly to add UI.

## auth-discovery

### graphify+crg — 84.0

Best strict answer for auth: complete and well grouped, though still long and with some broad/glob references.

Strengths: Strong workflow grouping across controller/service/MFA/external auth/routes/views.

Weaknesses: Long; a few view references are broad rather than exact; not all uncertainty is explicit.

### graphify — 80.0

Strong auth discovery answer with broad coverage and better completeness, but still a large map rather than a concise workflow-level synthesis.

Strengths: Covers primary auth flows, external auth, MFA, services, models, routes/views.

Weaknesses: Large and somewhat undifferentiated; caveats and source-confirmed vs inferred boundaries could be clearer.

### base — 77.0

Good broad auth map, but strict scoring penalizes candidate-map format and limited workflow narrative/uncertainty separation.

Strengths: Covers login, registration, password recovery, MFA, external auth and important files.

Weaknesses: More of a file inventory than a verified flow map; long and not sharply prioritized.

### crg — 72.0

Usable source-verified map, but under strict grading it is less precise and more directory-level than the leaders.

Strengths: Includes core controller/service and several auth-related areas.

Weaknesses: Some references are broad or directory-level; less polished organization and lower precision.

## order-total-impact

### base — 84.0

Best strict answer for impact: comprehensive and directly useful, though length keeps it below exceptional.

Strengths: Direct consumer table, contract/implementation/service/plugin/test surface, useful prioritization.

Weaknesses: Long and dense; some shortened display paths; could separate highest-risk changes more sharply.

### graphify+crg — 79.0

Strong broad map but not clearly better than base; strict grading penalizes length and unnecessary tool/method note.

Strengths: Good contract/order-processing/checkout/payment/discount/tax/test coverage.

Weaknesses: Long; Graphify note is irrelevant; not clearly more actionable than base.

### graphify — 78.0

Good impact map, but strict scoring penalizes density, weaker prioritization, and limited caveats.

Strengths: Covers contract, DI, order processing, checkout/payment/discount/tax/plugins/tests.

Weaknesses: Dense affected-surface list; not as risk-prioritized as base; limited caveats.

### crg — 70.0

Usable but not strong enough: relationship evidence is useful, but prioritization and direct-consumer framing lag behind.

Strengths: Good relationship-style coverage of checkout/payment/plugins/tax/discount/tests.

Weaknesses: Verbose; less structured; misses useful direct-consumer framing and priority ranking.

## cart-to-order-flow

### graphify — 87.0

Best strict answer overall: clear source-backed cart-to-order sequence, excellent transition point, still not perfect because caveats are limited.

Strengths: Clear sequence from add-to-cart through checkout/order creation; strong ShoppingCartItem-to-OrderItem conversion explanation.

Weaknesses: Could add more explicit uncertainty/caveat notes and distinguish core vs side-effect paths.

### base — 82.0

Strong flow trace with detailed source evidence, but strict grading penalizes density and lack of concise executive sequence.

Strengths: Detailed chain from route/controller to cart service, checkout, PlaceOrderAsync, post-processing and cleanup.

Weaknesses: Long and dense; could better isolate the minimal flow from supporting detail.

### graphify+crg — 74.0

Solid but not top-tier: covers the required flow, but lacks richness and precision compared with base/Graphify.

Strengths: Good high-level sequence through add-to-cart, checkout, PlaceOrderAsync, SaveOrderDetailsAsync, cleanup.

Weaknesses: Less detail on routes, payment request, post-processing, events, persistence; not enough evidence of combined-tool value.

### crg — 66.0

Weakest strict flow answer: it is usable, but the CRG advantage did not materialize and the answer itself is less complete.

Strengths: Covers main required files and broad flow stages.

Weaknesses: Less route/post-processing/persistence detail; final note about graph MCP cancellation undermines the run; weaker evidence depth.
