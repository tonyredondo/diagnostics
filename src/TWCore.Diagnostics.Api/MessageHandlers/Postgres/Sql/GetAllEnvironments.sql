-- Distinct of environment with not null using indexes.
WITH RECURSIVE t AS (
    SELECT MIN(environment) AS environment FROM logs
    UNION ALL
    SELECT (SELECT MIN(environment) FROM logs WHERE environment > t.environment)
    FROM t WHERE t.environment IS NOT NULL
)
SELECT environment FROM t WHERE environment IS NOT NULL ORDER BY environment;