import { useCallback, useEffect, useState } from 'react'

interface AsyncState<T> {
  data: T | null
  error: Error | null
  loading: boolean
  reload: () => void
}

export function useAsync<T>(load: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [data, setData] = useState<T | null>(null)
  const [error, setError] = useState<Error | null>(null)
  const [loading, setLoading] = useState(true)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    let active = true
    setLoading(true)
    load()
      .then((result) => {
        if (!active) return
        setData(result)
        setError(null)
      })
      .catch((reason: Error) => active && setError(reason))
      .finally(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [...deps, version])

  const reload = useCallback(() => setVersion((current) => current + 1), [])
  return { data, error, loading, reload }
}
