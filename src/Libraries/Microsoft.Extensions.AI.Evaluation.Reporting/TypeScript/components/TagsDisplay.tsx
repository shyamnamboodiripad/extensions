// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { makeStyles, tokens } from '@fluentui/react-components';

const useStyles = makeStyles({
  tagsContainer: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    marginBottom: '16px',
  },
  tagsRow: {
    display: 'flex',
    flexWrap: 'wrap',
    gap: '6px',
  },
  tagBubble: {
    padding: '2px 10px',
    borderRadius: '12px',
    backgroundColor: '#f0f0f0',
    fontSize: '0.75rem',
    cursor: 'pointer',
    transition: 'all 0.2s ease-in-out',
    border: '1px solid #ddd',
    lineHeight: '1.2',
    display: 'flex',
    alignItems: 'center', // Center text vertically
    justifyContent: 'center', // Also centers horizontally if needed
    ':hover': {
      opacity: 0.9,
      boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)'
    },
    '&.selected': {
      zIndex: 1,
      boxShadow: '0 4px 8px rgba(0, 0, 0, 0.15)',
      outline: `2px solid ${tokens.colorNeutralForeground3}`,
      outlineOffset: '0px',
      border: 'none'
    }
  },
  globalTagBubble: {
    backgroundColor: '#e6f7ff', // Light blue background for global tags
    border: '1px solid #91caff',
    ':hover': {
      backgroundColor: '#d6f0ff',
    },
    '&.selected': {
      backgroundColor: '#d6f0ff'
    }
  },
  clearFilterBubble: {
    backgroundColor: tokens.colorNeutralBackground5,
    color: tokens.colorNeutralForeground1,
    border: `1px solid ${tokens.colorNeutralStroke1}`,
    fontWeight: '500',
    ':hover': {
      backgroundColor: tokens.colorNeutralBackground5Hover,
      boxShadow: '0 2px 4px rgba(0, 0, 0, 0.1)'
    },
    '&.selected': {
      backgroundColor: tokens.colorNeutralBackground4,
      boxShadow: '0 4px 8px rgba(0, 0, 0, 0.15)',
      outline: `2px solid ${tokens.colorNeutralForeground3}`,
      outlineOffset: '0px',
      border: 'none'
    }
  },
});

// Identify global tags (tags that appear on every result) and regular tags
function categorizeTagsByFrequency(dataset: Dataset): { 
  globalTags: Array<{ tag: string, count: number }>;
  regularTags: Array<{ tag: string, count: number }>;
} {
  const tagCounts = new Map<string, number>();
  const totalResults = dataset.scenarioRunResults.length;
  
  dataset.scenarioRunResults.forEach(result => {
    if (result.tags) {
      result.tags.forEach(tag => {
        const currentCount = tagCounts.get(tag) || 0;
        tagCounts.set(tag, currentCount + 1);
      });
    }
  });
  
  const globalTags: Array<{ tag: string, count: number }> = [];
  const regularTags: Array<{ tag: string, count: number }> = [];
  
  // Separate global tags from regular tags
  tagCounts.forEach((count, tag) => {
    const tagInfo = { tag, count };
    if (count === totalResults) {
      globalTags.push(tagInfo);
    } else {
      regularTags.push(tagInfo);
    }
  });
  
  // Sort both arrays by frequency (highest first)
  globalTags.sort((a, b) => b.count - a.count);
  regularTags.sort((a, b) => b.count - a.count);
  
  return { globalTags, regularTags };
}

export interface TagsDisplayProps {
  dataset: Dataset;
}

export function TagsDisplay({ dataset, onTagClick, selectedTags, onClearFilters }: TagsDisplayProps & {
  onTagClick: (tag: string) => void;
  selectedTags: string[];
  onClearFilters: () => void;
}) {
  const classes = useStyles();
  const { globalTags, regularTags } = categorizeTagsByFrequency(dataset);

  if (globalTags.length === 0 && regularTags.length === 0) {
    return null;
  }

  const isSelected = (tag: string) => selectedTags.includes(tag);

  return (
    <div className={classes.tagsContainer}>
      <div className={classes.tagsRow}>
        {globalTags.length > 0 && globalTags.map(({ tag, count }) => (
          <div 
            key={tag} 
            className={`${classes.tagBubble} ${classes.globalTagBubble} ${isSelected(tag) ? 'selected' : ''}`}
            title={`${tag} (appears in all ${count} results) - Click to filter by this tag`}
            onClick={() => onTagClick(tag)}
          >
            {tag}
          </div>
        ))}

        {regularTags.length > 0 && regularTags.map(({ tag, count }) => (
          <div 
            key={tag} 
            className={`${classes.tagBubble} ${isSelected(tag) ? 'selected' : ''}`}
            title={`${tag} (${count} results) - Click to filter by this tag`}
            onClick={() => onTagClick(tag)}
          >
            {tag}
          </div>
        ))}

        {selectedTags.length > 0 && (
          <div
            className={`${classes.tagBubble} ${classes.clearFilterBubble}`}
            onClick={onClearFilters}
            title="Clear all filters"
          >
            Clear Filters
          </div>
        )}
      </div>
    </div>
  );
}