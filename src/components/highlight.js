import React from 'react'

import PropTypes from 'prop-types'

import './highlight.css'

const Highlight = (props) => {
  return (
    <div className="highlight-highlight">
      <span className="highlight-text1">{props.title}</span>
      <span className="highlight-text2">{props.description}</span>
    </div>
  )
}

Highlight.defaultProps = {
  description:
    'Sed do eiusmod tempor incididunt ut labore et dolore magna aliquat enim ad minim veniam, quis nostrud',
  title: 'Everything you get with Finbest',
}

Highlight.propTypes = {
  description: PropTypes.string,
  title: PropTypes.string,
}

export default Highlight
