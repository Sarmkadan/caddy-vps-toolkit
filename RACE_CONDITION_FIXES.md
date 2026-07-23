# Race Condition Fixes in Adaptive Load Balancer

## Summary

This document describes the race condition fixes implemented in the adaptive load balancer components to ensure thread-safety across concurrent request paths.

## Issues Identified and Fixed

### 1. `UpstreamMetricsWindow` - Missing Thread Safety

**File:** `Domain/Models/UpstreamMetricsWindow.cs`

**Problem:** The `Add()`, `Clear()`, and `Summarize()` methods were not thread-safe. Concurrent calls to `Record()` from multiple request paths could corrupt the internal queue and `WindowStartUtc` field.

**Solution:** Added a private `_syncLock` object and wrapped all mutable operations in `lock` statements:
- `Add()` - protects queue modification and enqueue operation
- `Clear()` - protects queue clearing and `WindowStartUtc` reset
- `Summarize()` - protects snapshot creation from the queue

**Impact:** Ensures that concurrent metric recordings from different threads cannot corrupt the metrics window.

### 2. `SlidingWindowMetricsAggregator` - Redundant Locking

**File:** `LoadBalancing/AdaptiveLoadBalancer.cs`

**Problem:** The aggregator was using per-upstream locks (`_locks` dictionary) even though the `UpstreamMetricsWindow` now handles its own synchronization internally.

**Solution:** Removed the redundant `_locks` field and all lock statements in `Record()`, `GetSummary()`, and `Reset()` methods. The window's internal lock now provides all necessary synchronization.

**Impact:** Simplified the code while maintaining (and improving) thread-safety. Reduced lock contention overhead.


### 3. `AdaptiveLoadBalancer.ComputeScore()` - Inconsistent Snapshots

**File:** `LoadBalancing/AdaptiveLoadBalancer.cs`

**Problem:** The `ComputeScore()` method was calling `_metrics.GetSummary()` and `_adaptiveWeights.GetValueOrDefault()` separately without ensuring consistency between the two reads. Between these two calls, concurrent `RecordOutcomeAsync()` calls could modify the adaptive weights, leading to inconsistent scoring.

**Solution:** Refactored `EvaluatePoolAsync()` to compute all scores in a single pass, ensuring that all servers are scored using the same snapshot of metrics and adaptive weights. This guarantees consistency across the entire pool evaluation.


**Impact:** Prevents race conditions where different servers in the same pool evaluation could have different adaptive weight snapshots.


### 4. `UpstreamSelector` - Improved Thread Safety

**File:** `LoadBalancing/UpstreamSelector.cs`

**Problem:** The `SelectLeastConnections()` and `SelectWeightedRandom()` methods read `ActiveConnections` from multiple servers. While reading an `int` is atomic in .NET, the methods could benefit from clearer intent and consistency.

**Solution:** 
- Used `Random.Shared` instead of creating new `Random` instances (better for thread-safety)
- Added explicit argument validation with `ArgumentNullException.ThrowIfNull()`
- Improved code comments to clarify thread-safety guarantees


**Impact:** More robust and maintainable code with clear thread-safety guarantees.


## Thread Safety Guarantees


### Before Fixes
- ✗ Concurrent `Record()` calls could corrupt `UpstreamMetricsWindow`
- ✗ Metrics aggregation had redundant locking overhead
- ✗ Pool evaluations could have inconsistent adaptive weight snapshots
- ✗ No explicit thread-safety guarantees in selector methods

### After Fixes
- ✓ `UpstreamMetricsWindow` is fully thread-safe with internal locking
- ✓ `SlidingWindowMetricsAggregator` delegates to thread-safe windows
- ✓ `AdaptiveLoadBalancer` ensures consistent snapshots across pool evaluations
- ✓ `UpstreamSelector` methods are thread-safe and maintainable

## Testing

The existing test suite validates the functionality. The build succeeds with all changes.

## Performance Considerations

- Removed redundant locking in `SlidingWindowMetricsAggregator` reduces lock contention
- Internal locking in `UpstreamMetricsWindow` is fine-grained (per-window) minimizing contention
- `EvaluatePoolAsync()` now computes scores in a single pass, which is more efficient than the previous approach


## Compatibility

All changes are backward compatible. The public APIs remain unchanged, and the behavior is now more correct under concurrency.
